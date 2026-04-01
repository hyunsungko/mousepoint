using System.Windows;
using System.Windows.Interop;
using MousePoint.Core;
using MousePoint.Input;
using MousePoint.Rendering;
using MousePoint.UI;

namespace MousePoint;

/// <summary>
/// 투명 오버레이 윈도우 — 앱의 핵심 연결 코드.
/// 글로벌 훅, 렌더러, 트레이 아이콘 등 모든 컴포넌트를 생성·연결한다.
/// </summary>
public partial class MainWindow : Window
{
    // --- 핵심 상태 ---
    private AppState _appState = null!;
    private ToolManager _toolManager = null!;

    // --- 입력 ---
    private GlobalMouseHook _mouseHook = null!;
    private GlobalKeyboardHook _keyboardHook = null!;

    // --- 렌더링 ---
    private LaserRenderer _laserRenderer = null!;
    private HighlighterRenderer _highlighterRenderer = null!;
    private FadeOutManager _fadeOutManager = null!;
    private ModeIndicator _modeIndicator = null!;

    // --- UI ---
    private TrayIconManager _trayIconManager = null!;
    private OnboardingOverlay _onboardingOverlay = null!;

    // --- 마지막 마우스 위치 (ModeIndicator 표시용) ---
    private int _lastMouseX;
    private int _lastMouseY;

    // --- DPI 스케일링: 물리 픽셀 → WPF DIU 변환 ---
    private double _dpiScaleX = 1.0;
    private double _dpiScaleY = 1.0;

    public MainWindow()
    {
        InitializeComponent();

        // SourceInitialized에서 HwndSource가 준비된 후 키보드 훅 생성
        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    // ──────────────────────── 초기화 ────────────────────────

    /// <summary>
    /// HwndSource가 준비된 시점. WS_EX 스타일 설정 + 키보드 훅 생성.
    /// </summary>
    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        if (PresentationSource.FromVisual(this) is not HwndSource hwndSource) return;
        IntPtr hwnd = hwndSource.Handle;

        // DPI 스케일 팩터 캐싱 (물리 픽셀 → WPF DIU 변환용)
        var dpiScale = hwndSource.CompositionTarget.TransformFromDevice;
        _dpiScaleX = dpiScale.M11;
        _dpiScaleY = dpiScale.M22;

        // WS_EX_TRANSPARENT: 기본적으로 클릭 통과
        int exStyle = (int)NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE);
        exStyle |= NativeMethods.WS_EX_TRANSPARENT;
        exStyle |= NativeMethods.WS_EX_TOOLWINDOW; // Alt+Tab에서 숨김
        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, (IntPtr)exStyle);

        // 키보드 훅 (RegisterHotKey는 Window Handle이 필요)
        _keyboardHook = new GlobalKeyboardHook(this);
        _keyboardHook.F9Pressed += OnF9Pressed;
        _keyboardHook.CtrlShift1Pressed += OnCtrlShift1;
        _keyboardHook.CtrlShift2Pressed += OnCtrlShift2;
        _keyboardHook.CtrlShift3Pressed += OnCtrlShift3;
    }

    /// <summary>
    /// 윈도우 로드 완료. 모든 컴포넌트를 생성하고 이벤트를 연결한다.
    /// </summary>
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // VirtualScreen 전체를 커버
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;

        // 핵심 상태
        _appState = new AppState();
        _toolManager = new ToolManager(_appState);

        // 상태 변경 이벤트
        _appState.ModeChanged += OnModeChanged;

        // 마우스 훅
        _mouseHook = new GlobalMouseHook(Dispatcher);
        _mouseHook.MouseMoved += OnMouseMoved;
        _mouseHook.LeftButtonDown += OnLeftButtonDown;
        _mouseHook.LeftButtonUp += OnLeftButtonUp;
        _mouseHook.XButtonDown += OnXButtonDown;

        // 페이드 아웃 매니저
        _fadeOutManager = new FadeOutManager();
        _fadeOutManager.Start();

        // 렌더러
        _laserRenderer = new LaserRenderer(OverlayCanvas);
        _highlighterRenderer = new HighlighterRenderer(OverlayCanvas, ActiveCanvas, _fadeOutManager);

        // 프리셋 변경 시 형광펜 색상/굵기 반영
        _toolManager.PresetChanged += OnPresetChanged;

        // 모드 인디케이터
        _modeIndicator = new ModeIndicator();

        // 트레이 아이콘
        _trayIconManager = new TrayIconManager(
            onLaserSelected: () => _appState.SetMode(ToolMode.Laser),
            onHighlighterSelected: () => _appState.SetMode(ToolMode.Highlighter),
            onExitClicked: () =>
            {
                // WinForms 스레드에서 호출될 수 있으므로 WPF Dispatcher로 마샬링
                Dispatcher.BeginInvoke(() =>
                {
                    _trayIconManager?.Dispose();
                    Application.Current.Shutdown();
                });
            });
        _trayIconManager.UpdateState(_appState.CurrentMode);

        // 온보딩 오버레이 (첫 실행 체크)
        // WS_EX_TRANSPARENT 윈도우는 키보드 포커스를 못 받으므로,
        // 글로벌 훅(F9, 마우스 클릭, 사이드 버튼)으로 닫기 처리
        _onboardingOverlay = new OnboardingOverlay();
        _onboardingOverlay.ShowIfFirstRun(OverlayCanvas, () => { });
    }

    // ──────────────────────── WS_EX_TRANSPARENT 토글 ────────────────────────

    /// <summary>
    /// 클릭 통과(투명) 여부를 설정한다.
    /// true: 마우스 클릭이 아래 윈도우로 통과
    /// false: 이 윈도우가 마우스 입력을 받음 (형광펜 드래그용)
    /// </summary>
    private static readonly System.Windows.Media.Brush TransparentBg =
        System.Windows.Media.Brushes.Transparent;
    private static readonly System.Windows.Media.Brush OpaqueHitTestBg =
        new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromArgb(1, 0, 0, 0)); // alpha 1/255 — 눈에 안 보이지만 hit test 통과

    static MainWindow()
    {
        OpaqueHitTestBg.Freeze();
    }

    private void SetClickThrough(bool transparent)
    {
        var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
        if (hwndSource == null) return;

        IntPtr hwnd = hwndSource.Handle;
        int exStyle = (int)NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE);

        if (transparent)
        {
            exStyle |= NativeMethods.WS_EX_TRANSPARENT;
            ActiveCanvas.Background = TransparentBg;
            ActiveCanvas.IsHitTestVisible = false;
        }
        else
        {
            exStyle &= ~NativeMethods.WS_EX_TRANSPARENT;
            ActiveCanvas.Background = OpaqueHitTestBg;
            ActiveCanvas.IsHitTestVisible = true;
        }

        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, (IntPtr)exStyle);
    }

    // ──────────────────────── 마우스 이벤트 라우팅 ────────────────────────

    /// <summary>물리 픽셀 좌표를 WPF DIU 좌표로 변환하여 Canvas 상대 좌표를 반환.</summary>
    private (double cx, double cy) ScreenToCanvas(int screenX, int screenY)
    {
        double diuX = screenX * _dpiScaleX;
        double diuY = screenY * _dpiScaleY;
        return (diuX - Left, diuY - Top);
    }

    /// <summary>온보딩이 표시 중이면 닫는다.</summary>
    private void TryDismissOnboarding()
    {
        if (_onboardingOverlay.IsShowing)
        {
            _onboardingOverlay.DismissIfShowing();
        }
    }

    private void OnMouseMoved(int x, int y)
    {
        _lastMouseX = x;
        _lastMouseY = y;

        var (cx, cy) = ScreenToCanvas(x, y);

        switch (_appState.CurrentMode)
        {
            case ToolMode.Laser:
                _laserRenderer.OnMouseMove(cx, cy);
                break;
            case ToolMode.Highlighter:
                _highlighterRenderer.OnMouseMove(cx, cy);
                break;
        }
    }

    private void OnLeftButtonDown(int x, int y)
    {
        TryDismissOnboarding();
        if (_appState.CurrentMode == ToolMode.Highlighter)
        {
            var (cx, cy) = ScreenToCanvas(x, y);
            _highlighterRenderer.OnLeftButtonDown(cx, cy);
        }
    }

    private void OnLeftButtonUp(int x, int y)
    {
        if (_appState.CurrentMode == ToolMode.Highlighter)
        {
            var (cx, cy) = ScreenToCanvas(x, y);
            _highlighterRenderer.OnLeftButtonUp(cx, cy);
        }
    }

    private void OnXButtonDown(int button)
    {
        TryDismissOnboarding();
        switch (button)
        {
            case NativeMethods.XBUTTON1:
                // 도구 순환: 비활성→레이저→형광펜→비활성
                _appState.CycleTool();
                break;
            case NativeMethods.XBUTTON2:
                // 프리셋(색상) 순환
                _toolManager.CyclePreset();
                break;
        }
    }

    // ──────────────────────── 키보드 이벤트 라우팅 ────────────────────────

    private void OnF9Pressed()
    {
        TryDismissOnboarding();
        _appState.ToggleActivation();
    }

    private void OnCtrlShift1()
    {
        _appState.SetMode(ToolMode.Laser);
    }

    private void OnCtrlShift2()
    {
        _appState.SetMode(ToolMode.Highlighter);
    }

    private void OnCtrlShift3()
    {
        _appState.SetMode(ToolMode.Inactive);
    }

    // ──────────────────────── 모드 변경 처리 ────────────────────────

    /// <summary>
    /// 모드가 변경될 때 렌더러 활성/비활성 + UI 업데이트 + 클릭 통과 토글.
    /// </summary>
    private void OnModeChanged(ToolMode oldMode, ToolMode newMode)
    {
        // 레이저 렌더러 활성/비활성 전환
        _laserRenderer.SetActive(newMode == ToolMode.Laser);

        // 형광펜에서 다른 모드로 전환 시 진행 중인 스트로크 취소
        if (oldMode == ToolMode.Highlighter && newMode != ToolMode.Highlighter)
        {
            _highlighterRenderer.CancelCurrentStroke();
        }

        // 클릭 통과: 형광펜 모드에서는 해제, 나머지는 클릭 통과
        SetClickThrough(newMode != ToolMode.Highlighter);

        // 모드 인디케이터 표시 (커서 근처, DPI 보정)
        var (indicatorX, indicatorY) = ScreenToCanvas(_lastMouseX, _lastMouseY);
        _modeIndicator.Show(OverlayCanvas, newMode, _toolManager.ColorIndex, indicatorX, indicatorY);

        // 트레이 아이콘 상태 업데이트
        _trayIconManager.UpdateState(newMode);
    }

    /// <summary>
    /// 프리셋 변경 시 형광펜 색상/굵기를 렌더러에 반영한다.
    /// </summary>
    private void OnPresetChanged(int colorIndex, int thicknessIndex)
    {
        var color = ColorPresets.GetHighlighterColor(colorIndex);
        var opacity = ColorPresets.GetHighlighterOpacity(colorIndex);
        var thickness = ColorPresets.GetThickness(thicknessIndex);

        _highlighterRenderer.SetColor(color, opacity);
        _highlighterRenderer.SetThickness(thickness);

        // 색상 변경 인디케이터 표시
        if (_appState.CurrentMode == ToolMode.Highlighter)
        {
            var (px, py) = ScreenToCanvas(_lastMouseX, _lastMouseY);
            _modeIndicator.Show(OverlayCanvas, ToolMode.Highlighter, colorIndex, px, py);
        }
    }

    // ──────────────────────── 정리 ────────────────────────

    private void OnClosed(object? sender, EventArgs e)
    {
        // 이벤트 구독 해제
        _appState.ModeChanged -= OnModeChanged;
        _toolManager.PresetChanged -= OnPresetChanged;

        // 렌더러 정리
        _laserRenderer?.Dispose();
        _fadeOutManager?.Dispose();

        // 입력 훅 정리
        _mouseHook?.Dispose();
        _keyboardHook?.Dispose();

        // UI 정리
        _trayIconManager?.Dispose();
    }
}

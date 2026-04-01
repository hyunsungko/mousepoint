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
    private RectangleRenderer _rectangleRenderer = null!;
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

        // DWM 기반 투명도: AllowsTransparency 대신 하드웨어 가속 유지
        hwndSource.CompositionTarget.BackgroundColor = System.Windows.Media.Color.FromArgb(0, 0, 0, 0);
        var margins = new NativeMethods.MARGINS
        {
            cxLeftWidth = -1, cxRightWidth = -1,
            cyTopHeight = -1, cyBottomHeight = -1
        };
        NativeMethods.DwmExtendFrameIntoClientArea(hwnd, ref margins);

        // WS_EX_TOOLWINDOW: Alt+Tab에서 숨김 (클릭 통과는 Hide/Show로 제어)
        int exStyle = (int)NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE);
        exStyle |= NativeMethods.WS_EX_TOOLWINDOW;
        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, (IntPtr)exStyle);

        // 키보드 훅 (RegisterHotKey는 Window Handle이 필요)
        _keyboardHook = new GlobalKeyboardHook(this);
        _keyboardHook.F9Pressed += OnF9Pressed;
        _keyboardHook.CtrlShift1Pressed += OnCtrlShift1;
        _keyboardHook.CtrlShift2Pressed += OnCtrlShift2;
        _keyboardHook.CtrlShift3Pressed += OnCtrlShift3;
        _keyboardHook.CtrlShift4Pressed += OnCtrlShift4;
        _keyboardHook.EscPressed += OnEscPressed;
        _keyboardHook.CtrlShiftQPressed += OnCtrlShiftQ;
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
        _mouseHook.MouseWheel += OnMouseWheel;

        // 페이드 아웃 매니저
        _fadeOutManager = new FadeOutManager();
        _fadeOutManager.Start();

        // 렌더러
        _laserRenderer = new LaserRenderer(OverlayCanvas);
        _highlighterRenderer = new HighlighterRenderer(OverlayCanvas, ActiveCanvas, _fadeOutManager);
        _rectangleRenderer = new RectangleRenderer(OverlayCanvas, ActiveCanvas, _fadeOutManager);

        // 프리셋 변경 시 형광펜 색상/굵기 반영
        _toolManager.PresetChanged += OnPresetChanged;
        _toolManager.LaserPresetChanged += OnLaserPresetChanged;

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
        _onboardingOverlay = new OnboardingOverlay();
        if (_onboardingOverlay.IsFirstRun())
        {
            // 첫 실행: 레이저 모드로 먼저 진입하여 윈도우를 활성화한 후 온보딩 표시
            // DWM 모드에서는 Inactive 상태면 윈도우가 숨겨져 온보딩이 안 보임
            _appState.SetMode(ToolMode.Laser);
            _onboardingOverlay.ShowIfFirstRun(OverlayCanvas, () => { });
        }
        else
        {
            // 이미 사용한 적 있으면 레이저 모드로 바로 시작
            _appState.SetMode(ToolMode.Laser);
        }
    }

    // ──────────────────────── 오버레이 표시/숨김 ────────────────────────

    /// <summary>
    /// DWM 모드에서는 클릭 통과가 안 되므로, Inactive일 때 윈도우를 숨겨서
    /// 사용자가 데스크톱을 자유롭게 사용할 수 있게 한다.
    /// </summary>
    private void SetOverlayVisible(bool visible)
    {
        if (visible)
        {
            Show();
            // VirtualScreen 위치 재설정 (모니터 변경 대응)
            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;
        }
        else
        {
            Hide();
        }
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
            case ToolMode.Rectangle:
                _rectangleRenderer.OnMouseMove(cx, cy);
                break;
        }
    }

    private void OnLeftButtonDown(int x, int y)
    {
        TryDismissOnboarding();
        var (cx, cy) = ScreenToCanvas(x, y);
        switch (_appState.CurrentMode)
        {
            case ToolMode.Highlighter:
                _highlighterRenderer.OnLeftButtonDown(cx, cy);
                break;
            case ToolMode.Rectangle:
                _rectangleRenderer.OnLeftButtonDown(cx, cy);
                break;
        }
    }

    private void OnLeftButtonUp(int x, int y)
    {
        var (cx, cy) = ScreenToCanvas(x, y);
        switch (_appState.CurrentMode)
        {
            case ToolMode.Highlighter:
                _highlighterRenderer.OnLeftButtonUp(cx, cy);
                break;
            case ToolMode.Rectangle:
                _rectangleRenderer.OnLeftButtonUp(cx, cy);
                break;
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

    private void OnMouseWheel(int delta)
    {
        switch (_appState.CurrentMode)
        {
            case ToolMode.Highlighter:
                _toolManager.CycleThickness(delta > 0);
                break;
            case ToolMode.Rectangle:
                _rectangleRenderer.CycleBorderThickness(delta > 0);
                var (rx, ry) = ScreenToCanvas(_lastMouseX, _lastMouseY);
                _modeIndicator.Show(OverlayCanvas, ToolMode.Rectangle, _toolManager.ColorIndex, rx, ry,
                    borderThickness: _rectangleRenderer.CurrentBorderThickness);
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

    private void OnCtrlShift4()
    {
        _appState.SetMode(ToolMode.Rectangle);
    }

    private void OnEscPressed()
    {
        if (_onboardingOverlay.IsShowing)
        {
            // 온보딩 표시 중이면 종료
            _trayIconManager?.Dispose();
            Application.Current.Shutdown();
        }
        else
        {
            // 활성 상태면 비활성화
            _appState.SetMode(ToolMode.Inactive);
        }
    }

    private void OnCtrlShiftQ()
    {
        _trayIconManager?.Dispose();
        Application.Current.Shutdown();
    }

    // ──────────────────────── 모드 변경 처리 ────────────────────────

    /// <summary>
    /// 모드가 변경될 때 렌더러 활성/비활성 + UI 업데이트 + 클릭 통과 토글.
    /// </summary>
    private void OnModeChanged(ToolMode oldMode, ToolMode newMode)
    {
        // 레이저 렌더러 활성/비활성 전환
        _laserRenderer.SetActive(newMode == ToolMode.Laser);

        // 형광펜/네모박스에서 다른 모드로 전환 시 진행 중인 드래그 취소
        if (oldMode == ToolMode.Highlighter && newMode != ToolMode.Highlighter)
            _highlighterRenderer.CancelCurrentStroke();
        if (oldMode == ToolMode.Rectangle && newMode != ToolMode.Rectangle)
            _rectangleRenderer.CancelCurrentRect();

        // 모드 인디케이터 표시 (Inactive 포함 모든 모드)
        if (newMode != ToolMode.Inactive)
        {
            SetOverlayVisible(true);
            var (indicatorX, indicatorY) = ScreenToCanvas(_lastMouseX, _lastMouseY);
            _modeIndicator.Show(OverlayCanvas, newMode, _toolManager.ColorIndex, indicatorX, indicatorY,
                _toolManager.ThicknessIndex, _toolManager.LaserColorIndex);
        }
        else
        {
            // 비활성 인디케이터를 잠깐 보여준 후 윈도우 숨김
            var (indicatorX, indicatorY) = ScreenToCanvas(_lastMouseX, _lastMouseY);
            _modeIndicator.Show(OverlayCanvas, ToolMode.Inactive, 0, indicatorX, indicatorY);

            var hideTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(600)
            };
            hideTimer.Tick += (_, _) =>
            {
                hideTimer.Stop();
                if (_appState.CurrentMode == ToolMode.Inactive)
                    SetOverlayVisible(false);
            };
            hideTimer.Start();
        }

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
        _rectangleRenderer.SetColor(color, opacity);

        // 색상 변경 인디케이터 표시
        if (_appState.CurrentMode == ToolMode.Highlighter)
        {
            var (px, py) = ScreenToCanvas(_lastMouseX, _lastMouseY);
            _modeIndicator.Show(OverlayCanvas, ToolMode.Highlighter, colorIndex, px, py, thicknessIndex);
        }
        else if (_appState.CurrentMode == ToolMode.Rectangle)
        {
            var (px, py) = ScreenToCanvas(_lastMouseX, _lastMouseY);
            _modeIndicator.Show(OverlayCanvas, ToolMode.Rectangle, colorIndex, px, py);
        }
    }

    /// <summary>
    /// 레이저 프리셋 변경 시 레이저 색상을 렌더러에 반영한다.
    /// </summary>
    private void OnLaserPresetChanged(int laserColorIndex)
    {
        var mainColor = ColorPresets.GetLaserColor(laserColorIndex);
        var glowColor = ColorPresets.GetLaserGlowColor(laserColorIndex);
        _laserRenderer.SetColor(mainColor, glowColor);

        // 레이저 색상 변경 인디케이터 표시
        if (_appState.CurrentMode == ToolMode.Laser)
        {
            var (px, py) = ScreenToCanvas(_lastMouseX, _lastMouseY);
            _modeIndicator.Show(OverlayCanvas, ToolMode.Laser, _toolManager.ColorIndex, px, py,
                _toolManager.ThicknessIndex, laserColorIndex);
        }
    }

    // ──────────────────────── 정리 ────────────────────────

    private void OnClosed(object? sender, EventArgs e)
    {
        // 이벤트 구독 해제
        _appState.ModeChanged -= OnModeChanged;
        _toolManager.PresetChanged -= OnPresetChanged;
        _toolManager.LaserPresetChanged -= OnLaserPresetChanged;
        _mouseHook.MouseWheel -= OnMouseWheel;

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

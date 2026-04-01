using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MousePoint.UI;

/// <summary>
/// 첫 실행 시 사용법 안내 오버레이.
/// %LOCALAPPDATA%\MousePoint\onboarding_done 파일 유무로 첫 실행 여부를 판별한다.
/// 아무 키 또는 마우스 클릭 시 오버레이가 사라지고 파일이 생성된다.
/// </summary>
public sealed class OnboardingOverlay
{
    /// <summary>온보딩 완료 마커 파일 경로.</summary>
    private static readonly string MarkerFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MousePoint",
        "onboarding_done");

    private Border? _overlayElement;
    private Canvas? _canvas;
    private Action? _onDismiss;

    /// <summary>온보딩 오버레이가 현재 표시 중인지 여부.</summary>
    public bool IsShowing => _overlayElement != null;

    /// <summary>첫 실행인지 확인 (마커 파일 유무).</summary>
    public bool IsFirstRun() => !File.Exists(MarkerFilePath);

    /// <summary>
    /// 첫 실행이면 안내 오버레이를 표시한다. 이미 사용한 적 있으면 아무 것도 하지 않는다.
    /// </summary>
    /// <param name="canvas">오버레이를 표시할 Canvas.</param>
    /// <param name="onDismiss">오버레이가 닫힐 때 호출되는 콜백.</param>
    /// <summary>
    /// 첫 실행이면 안내 오버레이를 표시한다. 이미 사용한 적 있으면 아무 것도 하지 않는다.
    /// 닫기는 WPF 이벤트 대신 외부에서 DismissIfShowing()을 호출해야 한다.
    /// (WS_EX_TRANSPARENT 윈도우는 키보드 포커스를 받을 수 없으므로)
    /// </summary>
    public void ShowIfFirstRun(Canvas canvas, Action onDismiss)
    {
        if (File.Exists(MarkerFilePath))
            return;

        _canvas = canvas;
        _onDismiss = onDismiss;

        var koText = new TextBlock
        {
            Text = string.Join("\n", new[]
            {
                "MousePoint",
                "",
                "🎯  F9 — 활성화 / 비활성화 (오버레이 ON/OFF)",
                "🔄  마우스 사이드 버튼 1 — 도구 순환 (레이저 → 형광펜 → 네모박스 → 비활성)",
                "🎨  마우스 사이드 버튼 2 — 색상 순환",
                "📏  스크롤 휠 — 형광펜 굵기 / 네모박스 테두리 굵기 조절",
                "",
                "⌨️  단축키",
                "    Ctrl+Shift+1  레이저     Ctrl+Shift+2  형광펜",
                "    Ctrl+Shift+3  비활성     Ctrl+Shift+4  네모박스",
                "    Ctrl+Shift+Q  종료",
                "",
                "아무 곳이나 클릭하거나 F9를 눌러 시작하세요",
            }),
            Foreground = Brushes.White,
            FontSize = 17,
            FontWeight = FontWeights.Medium,
            TextAlignment = TextAlignment.Center,
            LineHeight = 26
        };

        var enText = new TextBlock
        {
            Text = string.Join("\n", new[]
            {
                "",
                "F9 — Toggle overlay  |  Side Button 1 — Cycle tools  |  Side Button 2 — Cycle colors",
                "Scroll Wheel — Adjust thickness  |  Ctrl+Shift+Q — Quit",
            }),
            Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
            FontSize = 13,
            TextAlignment = TextAlignment.Center,
            LineHeight = 20
        };

        var escText = new TextBlock
        {
            Text = "\nESC — 종료 / Quit",
            Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
            FontSize = 12,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 10, 0, 0)
        };

        var creditText = new TextBlock
        {
            Text = "Made by DRT.Hs  |  github.com/hyunsungko/mousepoint",
            Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
            FontSize = 11,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 20, 0, 0)
        };

        var stack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Children = { koText, enText, escText, creditText }
        };

        // Canvas 전체를 덮고 중앙 정렬 (멀티모니터 대응)
        double canvasW = canvas.ActualWidth > 0 ? canvas.ActualWidth : SystemParameters.VirtualScreenWidth;
        double canvasH = canvas.ActualHeight > 0 ? canvas.ActualHeight : SystemParameters.VirtualScreenHeight;

        var grid = new Grid
        {
            Width = canvasW,
            Height = canvasH,
            Children = { stack }
        };

        _overlayElement = new Border
        {
            Background = new SolidColorBrush(Colors.Black) { Opacity = 0.7 },
            Child = grid,
            IsHitTestVisible = false,
            Width = canvasW,
            Height = canvasH
        };

        Canvas.SetLeft(_overlayElement, 0);
        Canvas.SetTop(_overlayElement, 0);
        canvas.Children.Add(_overlayElement);
    }

    /// <summary>
    /// 외부(글로벌 훅)에서 호출하여 온보딩을 닫는다.
    /// </summary>
    public void DismissIfShowing()
    {
        if (!IsShowing) return;
        Dismiss();
    }

    /// <summary>오버레이를 제거하고 마커 파일을 생성한다.</summary>
    private void Dismiss()
    {
        if (_overlayElement == null || _canvas == null) return;

        // Canvas에서 제거
        _canvas.Children.Remove(_overlayElement);
        _overlayElement = null;

        // 마커 파일 생성 (디렉토리가 없으면 생성)
        try
        {
            string? dir = Path.GetDirectoryName(MarkerFilePath);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(MarkerFilePath, DateTime.UtcNow.ToString("O"));
        }
        catch
        {
            // 파일 생성 실패는 무시 (다음 실행 시 다시 표시)
        }

        _onDismiss?.Invoke();
    }
}

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

    /// <summary>
    /// 첫 실행이면 안내 오버레이를 표시한다. 이미 사용한 적 있으면 아무 것도 하지 않는다.
    /// </summary>
    /// <param name="canvas">오버레이를 표시할 Canvas.</param>
    /// <param name="onDismiss">오버레이가 닫힐 때 호출되는 콜백.</param>
    public void ShowIfFirstRun(Canvas canvas, Action onDismiss)
    {
        if (File.Exists(MarkerFilePath))
            return;

        _canvas = canvas;
        _onDismiss = onDismiss;

        // 가이드 텍스트
        var guideText = new TextBlock
        {
            Text = string.Join("\n", new[]
            {
                "MousePoint에 오신 것을 환영합니다!",
                "",
                "🎯  F9 — 활성화 / 비활성화",
                "🔄  마우스 사이드 버튼 1 — 도구 전환",
                "🎨  마우스 사이드 버튼 2 — 색상 전환",
                "✏️  형광펜 모드에서 드래그하여 그리기",
                "",
                "아무 키나 눌러 시작하세요"
            }),
            Foreground = Brushes.White,
            FontSize = 18,
            FontWeight = FontWeights.Medium,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            LineHeight = 28
        };

        // 중앙 정렬 컨테이너
        var grid = new Grid
        {
            Width = SystemParameters.VirtualScreenWidth,
            Height = SystemParameters.VirtualScreenHeight,
            Children = { guideText }
        };

        // 반투명 검정 오버레이
        _overlayElement = new Border
        {
            Background = new SolidColorBrush(Colors.Black) { Opacity = 0.7 },
            Child = grid,
            IsHitTestVisible = true,
            Focusable = true,
            Width = SystemParameters.VirtualScreenWidth,
            Height = SystemParameters.VirtualScreenHeight
        };

        // 입력 이벤트 연결 (아무 키 또는 마우스 클릭)
        _overlayElement.KeyDown += OnInputReceived;
        _overlayElement.MouseDown += OnMouseInputReceived;

        Canvas.SetLeft(_overlayElement, 0);
        Canvas.SetTop(_overlayElement, 0);
        canvas.Children.Add(_overlayElement);

        // 키보드 포커스를 받도록 설정
        _overlayElement.Focus();
        Keyboard.Focus(_overlayElement);
    }

    /// <summary>키 입력 시 오버레이를 닫는다.</summary>
    private void OnInputReceived(object sender, KeyEventArgs e)
    {
        Dismiss();
    }

    /// <summary>마우스 클릭 시 오버레이를 닫는다.</summary>
    private void OnMouseInputReceived(object sender, MouseButtonEventArgs e)
    {
        Dismiss();
    }

    /// <summary>오버레이를 제거하고 마커 파일을 생성한다.</summary>
    private void Dismiss()
    {
        if (_overlayElement == null || _canvas == null) return;

        // 이벤트 해제
        _overlayElement.KeyDown -= OnInputReceived;
        _overlayElement.MouseDown -= OnMouseInputReceived;

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

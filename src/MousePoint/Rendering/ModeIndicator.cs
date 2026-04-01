using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MousePoint.Core;

namespace MousePoint.Rendering;

/// <summary>
/// 도구 전환 시 커서 근처에 0.5초간 현재 도구+색상을 표시하는 인디케이터.
/// 반투명 라운드 사각형 안에 아이콘+텍스트를 보여주고 자동으로 사라진다.
/// </summary>
public sealed class ModeIndicator
{
    /// <summary>인디케이터 표시 시간.</summary>
    private static readonly TimeSpan DisplayDuration = TimeSpan.FromMilliseconds(500);

    /// <summary>커서에서의 오프셋 (우측 하단).</summary>
    private const double OffsetX = 20;
    private const double OffsetY = 20;

    private DispatcherTimer? _timer;
    private Border? _currentIndicator;
    private Canvas? _currentCanvas;

    /// <summary>
    /// 커서 근처에 모드 인디케이터를 표시한다.
    /// </summary>
    /// <param name="canvas">오버레이 Canvas.</param>
    /// <param name="mode">현재 도구 모드.</param>
    /// <param name="colorIndex">형광펜 색상 인덱스.</param>
    /// <param name="screenX">스크린 X 좌표.</param>
    /// <param name="screenY">스크린 Y 좌표.</param>
    public void Show(Canvas canvas, ToolMode mode, int colorIndex, double screenX, double screenY)
    {
        // 기존 인디케이터가 있으면 제거
        RemoveCurrent();

        // 표시할 텍스트 + 색상 결정
        var (text, indicatorColor) = mode switch
        {
            ToolMode.Laser => ("🔴 레이저", ColorPresets.LaserColor),
            ToolMode.Highlighter => GetHighlighterLabel(colorIndex),
            ToolMode.Inactive => ("⏸ 비활성", Color.FromRgb(128, 128, 128)),
            _ => ("⏸ 비활성", Color.FromRgb(128, 128, 128))
        };

        // 반투명 라운드 사각형 + 텍스트 생성
        var textBlock = new TextBlock
        {
            Text = text,
            Foreground = Brushes.White,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(10, 5, 10, 5)
        };

        var border = new Border
        {
            Background = new SolidColorBrush(indicatorColor) { Opacity = 0.85 },
            CornerRadius = new CornerRadius(6),
            Child = textBlock,
            IsHitTestVisible = false
        };

        // 좌표는 이미 MainWindow에서 DPI 보정된 Canvas 좌표
        // 인디케이터 크기를 측정하여 화면 경계 클램핑
        border.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        double indicatorW = border.DesiredSize.Width;
        double indicatorH = border.DesiredSize.Height;

        double canvasW = canvas.ActualWidth > 0 ? canvas.ActualWidth : SystemParameters.VirtualScreenWidth;
        double canvasH = canvas.ActualHeight > 0 ? canvas.ActualHeight : SystemParameters.VirtualScreenHeight;

        // 기본: 커서 우측 하단. 넘치면 좌측/상단으로 뒤집기
        double canvasX = screenX + OffsetX;
        double canvasY = screenY + OffsetY;

        if (canvasX + indicatorW > canvasW)
            canvasX = screenX - OffsetX - indicatorW;
        if (canvasY + indicatorH > canvasH)
            canvasY = screenY - OffsetY - indicatorH;

        // 최소 0 보장
        canvasX = Math.Max(0, canvasX);
        canvasY = Math.Max(0, canvasY);

        Canvas.SetLeft(border, canvasX);
        Canvas.SetTop(border, canvasY);

        canvas.Children.Add(border);
        _currentIndicator = border;
        _currentCanvas = canvas;

        // 0.5초 후 자동 제거
        _timer?.Stop();
        _timer = new DispatcherTimer
        {
            Interval = DisplayDuration
        };
        _timer.Tick += (_, _) =>
        {
            RemoveCurrent();
            _timer.Stop();
        };
        _timer.Start();
    }

    /// <summary>형광펜 색상에 따른 라벨과 색상을 반환한다.</summary>
    private static (string text, Color color) GetHighlighterLabel(int colorIndex)
    {
        var preset = ColorPresets.GetHighlighterPreset(colorIndex);
        // 색상별 이모지 매핑
        string emoji = (colorIndex % ColorPresets.HighlighterColorCount) switch
        {
            0 => "🔴",  // 빨강
            1 => "🟡",  // 노랑
            2 => "🟢",  // 초록
            3 => "🔵",  // 파랑
            _ => "🟡"
        };

        return ($"{emoji} 형광펜 ({preset.Name})", preset.Color);
    }

    /// <summary>현재 표시 중인 인디케이터를 Canvas에서 제거한다.</summary>
    private void RemoveCurrent()
    {
        if (_currentIndicator != null && _currentCanvas != null)
        {
            _currentCanvas.Children.Remove(_currentIndicator);
            _currentIndicator = null;
            _currentCanvas = null;
        }
    }
}

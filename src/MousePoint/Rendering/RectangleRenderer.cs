using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using MousePoint.Core;

namespace MousePoint.Rendering;

/// <summary>
/// 형광색 네모박스 렌더러.
/// 드래그로 영역 선택 → 반투명 형광색 사각형 표시 → FadeOutManager로 자동 fade-out.
/// </summary>
public sealed class RectangleRenderer
{
    private static readonly TimeSpan RectLifetime = TimeSpan.FromSeconds(1.5);

    private readonly Canvas _completedCanvas;
    private readonly Canvas _activeCanvas;
    private readonly FadeOutManager _fadeOutManager;

    private Rectangle? _currentRect;
    private bool _isDragging;
    private double _startX, _startY;

    private static readonly double[] BorderThicknessOptions = [2.0, 4.0, 8.0];

    private SolidColorBrush _fillBrush;
    private SolidColorBrush _strokeBrush;
    private Color _color;
    private double _opacity;
    private int _borderThicknessIndex;

    public RectangleRenderer(Canvas completedCanvas, Canvas activeCanvas, FadeOutManager fadeOutManager)
    {
        _completedCanvas = completedCanvas ?? throw new ArgumentNullException(nameof(completedCanvas));
        _activeCanvas = activeCanvas ?? throw new ArgumentNullException(nameof(activeCanvas));
        _fadeOutManager = fadeOutManager ?? throw new ArgumentNullException(nameof(fadeOutManager));

        var preset = ColorPresets.GetHighlighterPreset(0);
        _color = preset.Color;
        _opacity = 0.1;
        _fillBrush = CreateFillBrush();
        _strokeBrush = CreateStrokeBrush();
    }

    private SolidColorBrush CreateFillBrush()
    {
        var brush = new SolidColorBrush(_color) { Opacity = _opacity };
        brush.Freeze();
        return brush;
    }

    private SolidColorBrush CreateStrokeBrush()
    {
        var brush = new SolidColorBrush(_color) { Opacity = _opacity + 0.3 };
        brush.Freeze();
        return brush;
    }

    public void SetColor(Color color, double opacity)
    {
        _color = color;
        _opacity = Math.Clamp(opacity, 0.0, 1.0) * 0.35;
        _fillBrush = CreateFillBrush();
        _strokeBrush = CreateStrokeBrush();
    }

    public void CycleBorderThickness(bool up)
    {
        _borderThicknessIndex = up
            ? (_borderThicknessIndex + 1) % BorderThicknessOptions.Length
            : (_borderThicknessIndex - 1 + BorderThicknessOptions.Length) % BorderThicknessOptions.Length;
    }

    public double CurrentBorderThickness => BorderThicknessOptions[_borderThicknessIndex];

    public void OnLeftButtonDown(double canvasX, double canvasY)
    {
        _startX = canvasX;
        _startY = canvasY;

        _currentRect = new Rectangle
        {
            Fill = _fillBrush,
            Stroke = _strokeBrush,
            StrokeThickness = BorderThicknessOptions[_borderThicknessIndex],
            RadiusX = 6,
            RadiusY = 6,
            IsHitTestVisible = false
        };

        Canvas.SetLeft(_currentRect, canvasX);
        Canvas.SetTop(_currentRect, canvasY);
        _currentRect.Width = 0;
        _currentRect.Height = 0;

        _activeCanvas.Children.Add(_currentRect);
        _isDragging = true;
    }

    public void OnMouseMove(double canvasX, double canvasY)
    {
        if (!_isDragging || _currentRect is null) return;

        double x = Math.Min(_startX, canvasX);
        double y = Math.Min(_startY, canvasY);
        double w = Math.Abs(canvasX - _startX);
        double h = Math.Abs(canvasY - _startY);

        Canvas.SetLeft(_currentRect, x);
        Canvas.SetTop(_currentRect, y);
        _currentRect.Width = w;
        _currentRect.Height = h;
    }

    public void OnLeftButtonUp(double canvasX, double canvasY)
    {
        if (!_isDragging || _currentRect is null) return;

        // 최종 크기 적용
        OnMouseMove(canvasX, canvasY);

        // 너무 작으면 무시 (실수 클릭 방지)
        if (_currentRect.Width < 5 && _currentRect.Height < 5)
        {
            _activeCanvas.Children.Remove(_currentRect);
            _isDragging = false;
            _currentRect = null;
            return;
        }

        // ActiveCanvas → CompletedCanvas
        _activeCanvas.Children.Remove(_currentRect);
        _currentRect.CacheMode = new BitmapCache();
        _completedCanvas.Children.Add(_currentRect);

        var annotation = new AnnotationElement(_currentRect, RectLifetime);
        _fadeOutManager.Register(annotation, _completedCanvas);

        _isDragging = false;
        _currentRect = null;
    }

    public void CancelCurrentRect()
    {
        if (!_isDragging || _currentRect is null) return;

        _activeCanvas.Children.Remove(_currentRect);
        _isDragging = false;
        _currentRect = null;
    }
}

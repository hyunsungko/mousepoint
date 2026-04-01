using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using MousePoint.Core;

namespace MousePoint.Rendering;

/// <summary>
/// 형광펜 렌더러.
/// PathGeometry 기반 반투명 스트로크. Brush는 frozen 캐시, 최소 거리 필터링 적용.
/// 드래그 완료 시 FadeOutManager에 등록하여 3초 후 자동 fade-out된다.
/// </summary>
public sealed class HighlighterRenderer
{
    private static readonly TimeSpan StrokeLifetime = TimeSpan.FromSeconds(3);

    /// <summary>포인트 간 최소 거리 (px²) — 이보다 가까운 이동은 무시.</summary>
    private const double MinDistSq = 4.0; // 2px

    private readonly Canvas _completedCanvas;  // 완성 스트로크 (하위 레이어)
    private readonly Canvas _activeCanvas;     // 드래그 중 (상위 레이어, 1개만)
    private readonly FadeOutManager _fadeOutManager;

    private Path? _currentPath;
    private PathFigure? _currentFigure;
    private bool _isDragging;
    private double _lastX, _lastY;

    private SolidColorBrush _cachedBrush;
    private Color _color;
    private double _opacity;
    private double _thickness;

    public HighlighterRenderer(Canvas completedCanvas, Canvas activeCanvas, FadeOutManager fadeOutManager)
    {
        _completedCanvas = completedCanvas ?? throw new ArgumentNullException(nameof(completedCanvas));
        _activeCanvas = activeCanvas ?? throw new ArgumentNullException(nameof(activeCanvas));
        _fadeOutManager = fadeOutManager ?? throw new ArgumentNullException(nameof(fadeOutManager));

        var preset = ColorPresets.GetHighlighterPreset(0);
        _color = preset.Color;
        _opacity = preset.Opacity;
        _thickness = preset.Thickness;
        _cachedBrush = CreateFrozenBrush();
    }

    private SolidColorBrush CreateFrozenBrush()
    {
        var brush = new SolidColorBrush(_color) { Opacity = _opacity };
        brush.Freeze();
        return brush;
    }

    public void SetColor(Color color, double opacity)
    {
        _color = color;
        _opacity = Math.Clamp(opacity, 0.0, 1.0);
        _cachedBrush = CreateFrozenBrush();
    }

    public void SetThickness(double thickness)
    {
        _thickness = Math.Max(1.0, thickness);
    }

    public void OnLeftButtonDown(double canvasX, double canvasY)
    {
        var pt = new Point(canvasX, canvasY);

        _currentFigure = new PathFigure
        {
            StartPoint = pt,
            IsClosed = false,
            IsFilled = false
        };

        var geometry = new PathGeometry();
        geometry.Figures.Add(_currentFigure);

        _currentPath = new Path
        {
            Data = geometry,
            Stroke = _cachedBrush,
            StrokeThickness = _thickness,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            IsHitTestVisible = false
        };

        _activeCanvas.Children.Add(_currentPath);
        _isDragging = true;
        _lastX = canvasX;
        _lastY = canvasY;
    }

    public void OnMouseMove(double canvasX, double canvasY)
    {
        if (!_isDragging || _currentFigure is null) return;

        double dx = canvasX - _lastX;
        double dy = canvasY - _lastY;
        if (dx * dx + dy * dy < MinDistSq) return;

        _currentFigure.Segments.Add(new LineSegment(new Point(canvasX, canvasY), true));
        _lastX = canvasX;
        _lastY = canvasY;
    }

    public void OnLeftButtonUp(double canvasX, double canvasY)
    {
        if (!_isDragging || _currentPath is null) return;

        _currentFigure?.Segments.Add(new LineSegment(new Point(canvasX, canvasY), true));

        // ActiveCanvas → OverlayCanvas로 이동 (레이어 분리)
        _activeCanvas.Children.Remove(_currentPath);
        _currentPath.CacheMode = new BitmapCache();
        _completedCanvas.Children.Add(_currentPath);

        var annotation = new AnnotationElement(_currentPath, StrokeLifetime);
        _fadeOutManager.Register(annotation, _completedCanvas);

        _isDragging = false;
        _currentPath = null;
        _currentFigure = null;
    }

    public void CancelCurrentStroke()
    {
        if (!_isDragging || _currentPath is null) return;

        _activeCanvas.Children.Remove(_currentPath);
        _isDragging = false;
        _currentPath = null;
        _currentFigure = null;
    }
}

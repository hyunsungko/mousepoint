using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MousePoint.Core;

namespace MousePoint.Rendering;

/// <summary>
/// 형광펜 렌더러.
/// PathGeometry 기반 반투명 스트로크. 완성 스트로크는 즉시 RenderTargetBitmap으로
/// 래스터화하여 Canvas 자식 수를 최소화하고 합성 비용을 고정.
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

    /// <summary>완성 스트로크를 누적하는 비트맵 이미지. Canvas에 Image 1개로 유지.</summary>
    private Image? _flattenedImage;

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

        // 드래그 중 fade-out 타이머 일시정지 — Dispatcher 경합 방지
        _fadeOutManager.Pause();
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
        if (!_isDragging || _currentPath is null || _currentFigure is null) return;

        _currentFigure.Segments.Add(new LineSegment(new Point(canvasX, canvasY), true));

        // ActiveCanvas → CompletedCanvas로 이동
        _activeCanvas.Children.Remove(_currentPath);
        _currentPath.CacheMode = new BitmapCache();
        _completedCanvas.Children.Add(_currentPath);

        // Douglas-Peucker로 스트로크 간소화
        var points = new List<Point>(_currentFigure.Segments.Count + 1)
        {
            _currentFigure.StartPoint
        };
        foreach (var segment in _currentFigure.Segments)
        {
            if (segment is LineSegment line)
                points.Add(line.Point);
        }

        var simplified = DouglasPeucker.Simplify(points, 2.0);

        if (simplified.Count >= 2 && simplified.Count < points.Count)
        {
            var newFigure = new PathFigure
            {
                StartPoint = simplified[0],
                IsClosed = false,
                IsFilled = false
            };
            for (int i = 1; i < simplified.Count; i++)
                newFigure.Segments.Add(new LineSegment(simplified[i], true));

            var newGeometry = new PathGeometry();
            newGeometry.Figures.Add(newFigure);
            _currentPath.Data = newGeometry;
        }

        var annotation = new AnnotationElement(_currentPath, StrokeLifetime);
        _fadeOutManager.Register(annotation, _completedCanvas);

        // fade-out 타이머 재개
        _fadeOutManager.Resume();

        _isDragging = false;
        _currentPath = null;
        _currentFigure = null;
    }

    /// <summary>
    /// 완성된 스트로크 Path를 기존 비트맵과 합성하여 단일 Image로 유지.
    /// Canvas에 Path가 누적되는 대신 Image 1개만 남아 합성 비용이 고정됨.
    /// </summary>
    private void FlattenStrokeIntoBitmap(Path strokePath)
    {
        double canvasW = _completedCanvas.ActualWidth;
        double canvasH = _completedCanvas.ActualHeight;
        if (canvasW <= 0 || canvasH <= 0) return;

        int pixelW = (int)Math.Ceiling(canvasW);
        int pixelH = (int)Math.Ceiling(canvasH);

        // 새 스트로크를 임시로 Canvas에 추가하여 렌더링
        _completedCanvas.Children.Add(strokePath);

        // Measure/Arrange를 강제 실행하여 렌더링 준비
        _completedCanvas.Measure(new Size(canvasW, canvasH));
        _completedCanvas.Arrange(new Rect(0, 0, canvasW, canvasH));

        // Canvas 전체(기존 비트맵 Image + 새 스트로크)를 RenderTargetBitmap으로 래스터화
        var rtb = new RenderTargetBitmap(pixelW, pixelH, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(_completedCanvas);
        rtb.Freeze();

        // Canvas의 모든 자식 제거 (기존 _flattenedImage + 새 스트로크 포함)
        _completedCanvas.Children.Clear();

        // 새 비트맵 Image 생성하여 Canvas에 단일 자식으로 배치
        _flattenedImage = new Image
        {
            Source = rtb,
            Width = canvasW,
            Height = canvasH,
            IsHitTestVisible = false,
            CacheMode = new BitmapCache()
        };
        _completedCanvas.Children.Add(_flattenedImage);

        // FadeOutManager에 등록 (비트맵 Image의 opacity를 fade-out)
        var annotation = new AnnotationElement(_flattenedImage, StrokeLifetime);
        _fadeOutManager.Register(annotation, _completedCanvas);
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

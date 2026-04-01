using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using MousePoint.Core;

namespace MousePoint.Rendering;

/// <summary>
/// 형광펜 렌더러.
/// Path 기반 반투명 스트로크를 그린다 (InkCanvas 미사용).
/// 드래그 완료 시 FadeOutManager에 등록하여 3초 후 자동 fade-out된다.
/// </summary>
public sealed class HighlighterRenderer
{
    /// <summary>형광펜 스트로크 기본 수명 (초).</summary>
    private static readonly TimeSpan StrokeLifetime = TimeSpan.FromSeconds(3);

    private readonly Canvas _canvas;
    private readonly FadeOutManager _fadeOutManager;

    // --- 현재 드래그 중인 스트로크 상태 ---
    private Path? _currentPath;
    private PathFigure? _currentFigure;
    private bool _isDragging;

    // --- 스타일 설정 ---
    private Color _color;
    private double _opacity;
    private double _thickness;

    public HighlighterRenderer(Canvas canvas, FadeOutManager fadeOutManager)
    {
        _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
        _fadeOutManager = fadeOutManager ?? throw new ArgumentNullException(nameof(fadeOutManager));

        // 기본값: 첫 번째 프리셋
        var preset = ColorPresets.GetHighlighterPreset(0);
        _color = preset.Color;
        _opacity = preset.Opacity;
        _thickness = preset.Thickness;
    }

    // ─────────────────────────── 스타일 설정 ───────────────────────────

    /// <summary>형광펜 색상과 불투명도를 설정한다.</summary>
    public void SetColor(Color color, double opacity)
    {
        _color = color;
        _opacity = Math.Clamp(opacity, 0.0, 1.0);
    }

    /// <summary>형광펜 두께를 설정한다.</summary>
    public void SetThickness(double thickness)
    {
        _thickness = Math.Max(1.0, thickness);
    }

    // ─────────────────────────── 드래그 이벤트 ───────────────────────────

    /// <summary>
    /// 좌클릭 다운: 새 Path + PathFigure를 시작한다.
    /// </summary>
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
            Stroke = new SolidColorBrush(_color) { Opacity = _opacity },
            StrokeThickness = _thickness,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            IsHitTestVisible = false
        };

        _canvas.Children.Add(_currentPath);
        _isDragging = true;
    }

    /// <summary>
    /// 마우스 이동: 현재 PathFigure에 LineSegment를 추가한다.
    /// </summary>
    public void OnMouseMove(double canvasX, double canvasY)
    {
        if (!_isDragging || _currentFigure is null) return;

        var pt = new Point(canvasX, canvasY);
        _currentFigure.Segments.Add(new LineSegment(pt, isStroked: true));
    }

    /// <summary>
    /// 좌클릭 업: Path를 완성하고 FadeOutManager에 등록한다.
    /// </summary>
    public void OnLeftButtonUp(double canvasX, double canvasY)
    {
        if (!_isDragging || _currentPath is null) return;

        // 마지막 포인트 추가
        var pt = new Point(canvasX, canvasY);
        _currentFigure?.Segments.Add(new LineSegment(pt, isStroked: true));

        // FadeOutManager에 등록 → lifetime 후 자동 fade-out
        var annotation = new AnnotationElement(_currentPath, StrokeLifetime);
        _fadeOutManager.Register(annotation, _canvas);

        _isDragging = false;
        _currentPath = null;
        _currentFigure = null;
    }

    /// <summary>
    /// 드래그 중 모드 전환 시 현재 스트로크를 취소하고 Canvas에서 제거한다.
    /// </summary>
    public void CancelCurrentStroke()
    {
        if (!_isDragging || _currentPath is null) return;

        _canvas.Children.Remove(_currentPath);
        _isDragging = false;
        _currentPath = null;
        _currentFigure = null;
    }
}

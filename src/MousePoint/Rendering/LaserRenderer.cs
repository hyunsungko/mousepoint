using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using MousePoint.Core;

namespace MousePoint.Rendering;

/// <summary>
/// 레이저 포인터 렌더러.
/// 링 버퍼에 최근 N개 마우스 좌표를 저장하고, CompositionTarget.Rendering 프레임마다
/// 자신이 관리하는 요소만 갱신하여 fade-out trail과 발광 원을 그린다.
/// </summary>
public sealed class LaserRenderer : IDisposable
{
    /// <summary>링 버퍼에 보관할 최대 포인트 수.</summary>
    private const int DefaultBufferSize = 60;

    private readonly Canvas _canvas;

    // --- 링 버퍼 ---
    private readonly (double x, double y)[] _buffer;
    private int _head;   // 다음 쓰기 위치
    private int _count;  // 현재 저장된 포인트 수

    // --- 캐시된 WPF 요소 (매 프레임 재생성 비용 절감) ---
    private readonly Ellipse _pointer;

    // --- 객체 풀: Line + Brush를 재사용하여 GC 압박 최소화 ---
    private readonly Line[] _trailPool;
    private int _visibleTrailCount;
    private bool _pointerOnCanvas;

    private bool _active;
    private bool _disposed;

    public LaserRenderer(Canvas canvas, int bufferSize = DefaultBufferSize)
    {
        _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
        _buffer = new (double, double)[bufferSize];

        // 포인터 원: 빨간색 + DropShadow 발광 효과
        var pointerBrush = new SolidColorBrush(ColorPresets.LaserColor);
        pointerBrush.Freeze();

        _pointer = new Ellipse
        {
            Width = ColorPresets.LaserRadius * 2,
            Height = ColorPresets.LaserRadius * 2,
            Fill = pointerBrush,
            Effect = new DropShadowEffect
            {
                Color = ColorPresets.LaserColor,
                BlurRadius = 15,
                ShadowDepth = 0,
                Opacity = 1.0
            },
            IsHitTestVisible = false
        };

        // Line 풀 사전 생성 (bufferSize - 1개, 인접 포인트 사이 선분)
        _trailPool = new Line[Math.Max(1, bufferSize - 1)];
        for (int i = 0; i < _trailPool.Length; i++)
        {
            _trailPool[i] = new Line
            {
                Stroke = new SolidColorBrush(ColorPresets.LaserColor),
                StrokeThickness = ColorPresets.LaserTrailWidth,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                IsHitTestVisible = false
            };
        }
    }

    // ─────────────────────────── 좌표 변환 ───────────────────────────

    /// <summary>스크린 좌표 → Canvas 상대 좌표.</summary>
    private static (double cx, double cy) ToCanvas(int screenX, int screenY)
    {
        return (
            screenX - SystemParameters.VirtualScreenLeft,
            screenY - SystemParameters.VirtualScreenTop
        );
    }

    // ─────────────────────────── 공개 API ───────────────────────────

    /// <summary>마우스 훅에서 호출. 링 버퍼에 포인트를 추가한다.</summary>
    public void OnMouseMove(int screenX, int screenY)
    {
        if (!_active) return;

        var (cx, cy) = ToCanvas(screenX, screenY);
        _buffer[_head] = (cx, cy);
        _head = (_head + 1) % _buffer.Length;
        if (_count < _buffer.Length) _count++;
    }

    /// <summary>렌더링을 시작하거나 중지한다.</summary>
    public void SetActive(bool active)
    {
        if (_active == active) return;
        _active = active;

        if (active)
        {
            CompositionTarget.Rendering += OnRendering;
        }
        else
        {
            CompositionTarget.Rendering -= OnRendering;
            ClearCanvas();
        }
    }

    // ─────────────────────────── 프레임 렌더링 ───────────────────────────

    /// <summary>
    /// 매 vsync 프레임마다 호출. 자신이 관리하는 요소만 갱신한다.
    /// 다른 렌더러(HighlighterRenderer 등)의 Canvas 요소를 건드리지 않는다.
    /// </summary>
    private void OnRendering(object? sender, EventArgs e)
    {
        // 이전 프레임에서 보이던 trail 라인을 Canvas에서 제거
        for (int i = 0; i < _visibleTrailCount; i++)
            _canvas.Children.Remove(_trailPool[i]);
        _visibleTrailCount = 0;

        if (_pointerOnCanvas)
        {
            _canvas.Children.Remove(_pointer);
            _pointerOnCanvas = false;
        }

        if (_count == 0) return;

        // 링 버퍼에서 오래된 순서대로 읽기
        int startIdx = (_count < _buffer.Length)
            ? 0
            : _head;

        int total = _count;
        double prevX = 0, prevY = 0;
        bool hasPrev = false;
        int lineIdx = 0;

        for (int i = 0; i < total; i++)
        {
            int idx = (startIdx + i) % _buffer.Length;
            var (px, py) = _buffer[idx];

            double opacity = (total == 1) ? 1.0 : (double)i / (total - 1);

            if (hasPrev && lineIdx < _trailPool.Length)
            {
                var line = _trailPool[lineIdx];
                line.X1 = prevX;
                line.Y1 = prevY;
                line.X2 = px;
                line.Y2 = py;
                ((SolidColorBrush)line.Stroke).Opacity = opacity;
                _canvas.Children.Add(line);
                lineIdx++;
            }

            prevX = px;
            prevY = py;
            hasPrev = true;
        }

        _visibleTrailCount = lineIdx;

        // 최신 위치에 포인터 원 배치
        if (total > 0)
        {
            int latestIdx = (_head - 1 + _buffer.Length) % _buffer.Length;
            var (lx, ly) = _buffer[latestIdx];

            Canvas.SetLeft(_pointer, lx - ColorPresets.LaserRadius);
            Canvas.SetTop(_pointer, ly - ColorPresets.LaserRadius);
            _canvas.Children.Add(_pointer);
            _pointerOnCanvas = true;
        }
    }

    /// <summary>자신이 관리하는 요소만 Canvas에서 제거하고 링 버퍼를 초기화한다.</summary>
    private void ClearCanvas()
    {
        for (int i = 0; i < _visibleTrailCount; i++)
            _canvas.Children.Remove(_trailPool[i]);
        _visibleTrailCount = 0;

        if (_pointerOnCanvas)
        {
            _canvas.Children.Remove(_pointer);
            _pointerOnCanvas = false;
        }

        _head = 0;
        _count = 0;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        SetActive(false);
    }
}

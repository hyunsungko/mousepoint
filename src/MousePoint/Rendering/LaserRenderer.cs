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
/// Canvas를 갱신하여 fade-out trail과 발광 원을 그린다.
/// </summary>
public sealed class LaserRenderer
{
    /// <summary>링 버퍼에 보관할 최대 포인트 수.</summary>
    private const int DefaultBufferSize = 60;

    private readonly Canvas _canvas;

    // --- 링 버퍼 ---
    private readonly (double x, double y, DateTime timestamp)[] _buffer;
    private int _head;   // 다음 쓰기 위치
    private int _count;  // 현재 저장된 포인트 수

    // --- 캐시된 WPF 요소 (매 프레임 재생성 비용 절감) ---
    private readonly Ellipse _pointer;

    private bool _active;

    public LaserRenderer(Canvas canvas, int bufferSize = DefaultBufferSize)
    {
        _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
        _buffer = new (double, double, DateTime)[bufferSize];

        // 포인터 원: 빨간색 + DropShadow 발광 효과
        _pointer = new Ellipse
        {
            Width = ColorPresets.LaserRadius * 2,   // 반지름 6px → 지름 12px
            Height = ColorPresets.LaserRadius * 2,
            Fill = new SolidColorBrush(ColorPresets.LaserColor),
            Effect = new DropShadowEffect
            {
                Color = ColorPresets.LaserColor,
                BlurRadius = 15,
                ShadowDepth = 0,       // 그림자 없이 발광만
                Opacity = 1.0
            },
            IsHitTestVisible = false
        };
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
        _buffer[_head] = (cx, cy, DateTime.UtcNow);
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

    /// <summary>매 vsync 프레임마다 호출. Canvas를 비우고 trail + 포인터를 다시 그린다.</summary>
    private void OnRendering(object? sender, EventArgs e)
    {
        if (_count == 0) return;

        _canvas.Children.Clear();

        var now = DateTime.UtcNow;

        // 링 버퍼에서 오래된 순서대로 읽기
        int startIdx = (_count < _buffer.Length)
            ? 0
            : _head; // 버퍼가 꽉 찼으면 head가 가장 오래된 위치

        // 포인트를 시간순 배열로 복사 (오래된 → 최신)
        int total = _count;
        double prevX = 0, prevY = 0;
        bool hasPrev = false;

        for (int i = 0; i < total; i++)
        {
            int idx = (startIdx + i) % _buffer.Length;
            var (px, py, ts) = _buffer[idx];

            // 선형 보간: 오래된 포인트(i=0)일수록 투명, 최신(i=total-1)일수록 불투명
            double opacity = (total == 1) ? 1.0 : (double)i / (total - 1);

            if (hasPrev)
            {
                var line = new Line
                {
                    X1 = prevX,
                    Y1 = prevY,
                    X2 = px,
                    Y2 = py,
                    Stroke = new SolidColorBrush(ColorPresets.LaserColor) { Opacity = opacity },
                    StrokeThickness = ColorPresets.LaserTrailWidth,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    IsHitTestVisible = false
                };
                _canvas.Children.Add(line);
            }

            prevX = px;
            prevY = py;
            hasPrev = true;
        }

        // 최신 위치에 포인터 원 배치
        if (total > 0)
        {
            int latestIdx = (_head - 1 + _buffer.Length) % _buffer.Length;
            var (lx, ly, _) = _buffer[latestIdx];

            Canvas.SetLeft(_pointer, lx - ColorPresets.LaserRadius);
            Canvas.SetTop(_pointer, ly - ColorPresets.LaserRadius);
            _canvas.Children.Add(_pointer);
        }
    }

    /// <summary>Canvas를 비우고 링 버퍼를 초기화한다.</summary>
    private void ClearCanvas()
    {
        _canvas.Children.Clear();
        _head = 0;
        _count = 0;
    }
}

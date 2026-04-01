using System.Windows.Controls;
using System.Windows.Threading;

namespace MousePoint.Rendering;

/// <summary>
/// DispatcherTimer(30fps)로 fade-out을 처리하는 관리자.
/// CompositionTarget.Rendering(60fps) 대신 사용하여 렌더링 부하 절감.
/// </summary>
public sealed class FadeOutManager : IDisposable
{
    private const double FadeDurationSeconds = 1.0;

    private readonly record struct Entry(AnnotationElement Element, Canvas Canvas);

    private readonly List<Entry> _entries = [];
    private readonly DispatcherTimer _timer;
    private bool _disposed;

    public FadeOutManager()
    {
        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(33) // ~30fps
        };
        _timer.Tick += OnTick;
    }

    public void Register(AnnotationElement element, Canvas canvas)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(canvas);

        _entries.Add(new Entry(element, canvas));

        if (!_timer.IsEnabled)
            _timer.Start();
    }

    public void Start() { } // 호환성 유지 — timer는 Register 시 자동 시작
    public void Stop() => _timer.Stop();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _entries.Clear();
    }

    /// <summary>
    /// 매 프레임 호출. 각 요소의 경과 시간에 따라 opacity를 갱신하고
    /// 만료된 요소를 Canvas에서 제거한다.
    /// </summary>
    private void OnTick(object? sender, EventArgs e)
    {
        if (_entries.Count == 0)
        {
            _timer.Stop();
            return;
        }

        var now = DateTime.UtcNow;
        int i = 0;

        while (i < _entries.Count)
        {
            var entry = _entries[i];
            double elapsedSec = (now - entry.Element.CreatedAt).TotalSeconds;
            double lifetimeSec = entry.Element.Lifetime.TotalSeconds;

            if (elapsedSec <= lifetimeSec)
            {
                i++;
                continue;
            }

            double fadeProgress = (elapsedSec - lifetimeSec) / FadeDurationSeconds;
            double newOpacity = 1.0 - fadeProgress;

            if (newOpacity <= 0.0)
            {
                // 만료: Canvas에서 제거 + swap-remove (O(1))
                entry.Element.UpdateOpacity(0);
                entry.Canvas.Children.Remove(entry.Element.Visual);
                _entries[i] = _entries[^1];
                _entries.RemoveAt(_entries.Count - 1);
                // i 증가 안 함 — swap된 요소 처리 필요
            }
            else
            {
                entry.Element.UpdateOpacity(newOpacity);
                i++;
            }
        }
    }
}

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
    private const int MaxConcurrent = 6;

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

        // 동시 표시 제한 초과 시 가장 오래된 항목 즉시 제거
        while (_entries.Count > MaxConcurrent)
        {
            int oldestIndex = 0;
            var oldestTime = _entries[0].Element.CreatedAt;
            for (int i = 1; i < _entries.Count - 1; i++) // 방금 추가한 것은 제외
            {
                if (_entries[i].Element.CreatedAt < oldestTime)
                {
                    oldestTime = _entries[i].Element.CreatedAt;
                    oldestIndex = i;
                }
            }

            var oldest = _entries[oldestIndex];
            oldest.Element.UpdateOpacity(0);
            oldest.Canvas.Children.Remove(oldest.Element.Visual);
            _entries[oldestIndex] = _entries[^1];
            _entries.RemoveAt(_entries.Count - 1);
        }

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

        // 만료 항목을 먼저 수집, 루프 종료 후 일괄 제거
        List<Entry>? expired = null;
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
                // 만료: swap-remove로 리스트에서 제거, Canvas 제거는 후처리
                entry.Element.UpdateOpacity(0);
                (expired ??= []).Add(entry);
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

        // 일괄 Canvas.Children.Remove()
        if (expired is not null)
        {
            foreach (var entry in expired)
                entry.Canvas.Children.Remove(entry.Element.Visual);
        }
    }
}

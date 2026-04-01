using System.Windows.Controls;
using System.Windows.Media;

namespace MousePoint.Rendering;

/// <summary>
/// CompositionTarget.Rendering에 연결되어 매 프레임 fade-out을 처리하는 관리자.
/// 등록된 AnnotationElement의 lifetime이 경과하면 약 1초에 걸쳐 opacity를 0으로 감소시키고,
/// 완전히 투명해지면 Canvas에서 제거한다.
/// </summary>
public sealed class FadeOutManager : IDisposable
{
    /// <summary>fade-out에 소요되는 시간 (초).</summary>
    private const double FadeDurationSeconds = 1.0;

    /// <summary>관리 대상 요소와 소속 Canvas를 묶는 내부 레코드.</summary>
    private readonly record struct Entry(AnnotationElement Element, Canvas Canvas);

    private readonly List<Entry> _entries = [];
    private bool _running;
    private bool _disposed;

    /// <summary>
    /// 주석 요소를 등록한다. Lifetime 경과 후 자동으로 fade-out된다.
    /// </summary>
    public void Register(AnnotationElement element, Canvas canvas)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(canvas);

        _entries.Add(new Entry(element, canvas));
    }

    /// <summary>CompositionTarget.Rendering 구독을 시작한다.</summary>
    public void Start()
    {
        if (_running) return;
        _running = true;
        CompositionTarget.Rendering += OnRendering;
    }

    /// <summary>CompositionTarget.Rendering 구독을 해제한다.</summary>
    public void Stop()
    {
        if (!_running) return;
        _running = false;
        CompositionTarget.Rendering -= OnRendering;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _entries.Clear();
    }

    /// <summary>
    /// 매 프레임 호출. 각 요소의 경과 시간에 따라 opacity를 갱신하고
    /// 만료된 요소를 Canvas에서 제거한다.
    /// </summary>
    private void OnRendering(object? sender, EventArgs e)
    {
        var now = DateTime.UtcNow;

        // 역순 순회하여 제거 시 인덱스 꼬임 방지
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            var entry = _entries[i];
            var elapsed = now - entry.Element.CreatedAt;
            var lifetimeSeconds = entry.Element.Lifetime.TotalSeconds;

            if (elapsed.TotalSeconds <= lifetimeSeconds)
            {
                // 아직 lifetime 이내 — 완전 불투명 유지
                continue;
            }

            // fade-out 진행률 (0.0 → 1.0)
            double fadeProgress = (elapsed.TotalSeconds - lifetimeSeconds) / FadeDurationSeconds;
            double newOpacity = Math.Max(0.0, 1.0 - fadeProgress);
            entry.Element.UpdateOpacity(newOpacity);

            if (entry.Element.IsExpired())
            {
                // Canvas에서 시각 요소 제거 + 리스트에서 제거
                entry.Canvas.Children.Remove(entry.Element.Visual);
                _entries.RemoveAt(i);
            }
        }
    }
}

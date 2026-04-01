using System.Windows;

namespace MousePoint.Rendering;

/// <summary>
/// 캔버스에 표시되는 주석 요소의 베이스 클래스.
/// 생성 시각, 수명, 불투명도를 관리하며 FadeOutManager가 매 프레임 갱신한다.
/// </summary>
public class AnnotationElement
{
    /// <summary>Canvas에 추가되는 WPF 시각 요소.</summary>
    public UIElement Visual { get; }

    /// <summary>요소가 생성된 시각.</summary>
    public DateTime CreatedAt { get; }

    /// <summary>현재 불투명도 (1.0 = 완전 불투명, 0.0 = 완전 투명).</summary>
    public double Opacity { get; private set; }

    /// <summary>
    /// 요소가 화면에 유지되는 시간.
    /// Lifetime 경과 후 fade-out이 시작된다.
    /// </summary>
    public TimeSpan Lifetime { get; }

    public AnnotationElement(UIElement visual, TimeSpan lifetime)
    {
        Visual = visual ?? throw new ArgumentNullException(nameof(visual));
        Lifetime = lifetime;
        CreatedAt = DateTime.UtcNow;
        Opacity = 1.0;
    }

    /// <summary>
    /// Lifetime + fade-out 시간(기본 1초)이 모두 경과했는지 확인.
    /// opacity가 0 이하이면 만료된 것으로 간주한다.
    /// </summary>
    public bool IsExpired() => Opacity <= 0.0;

    /// <summary>
    /// 불투명도를 갱신하고 Visual에도 반영한다.
    /// </summary>
    public void UpdateOpacity(double newOpacity)
    {
        Opacity = Math.Clamp(newOpacity, 0.0, 1.0);
        Visual.Opacity = Opacity;
    }
}

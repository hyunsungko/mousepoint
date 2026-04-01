using System.Windows.Media;
using MousePoint.Core;
using Xunit;

namespace MousePoint.Tests.Core;

public class ColorPresetsTests
{
    // --- 카운트 검증 ---

    [Fact]
    public void HighlighterColorCount_4개()
    {
        Assert.Equal(4, ColorPresets.HighlighterColorCount);
    }

    [Fact]
    public void ThicknessCount_3개()
    {
        Assert.Equal(3, ColorPresets.ThicknessCount);
    }

    // --- 레이저 상수 검증 ---

    [Fact]
    public void LaserColor_빨강()
    {
        Assert.Equal(Color.FromRgb(255, 0, 0), ColorPresets.LaserColor);
    }

    [Fact]
    public void LaserRadius_6()
    {
        Assert.Equal(6.0, ColorPresets.LaserRadius);
    }

    [Fact]
    public void LaserTrailWidth_3()
    {
        Assert.Equal(3.0, ColorPresets.LaserTrailWidth);
    }

    // --- GetHighlighterPreset: 각 프리셋 값 검증 ---

    [Fact]
    public void GetHighlighterPreset_0_빨강()
    {
        var preset = ColorPresets.GetHighlighterPreset(0);
        Assert.Equal("빨강", preset.Name);
        Assert.Equal(Color.FromRgb(255, 59, 48), preset.Color);
        Assert.Equal(0.3, preset.Opacity);
        Assert.Equal(6.0, preset.Thickness);
    }

    [Fact]
    public void GetHighlighterPreset_1_노랑()
    {
        var preset = ColorPresets.GetHighlighterPreset(1);
        Assert.Equal("노랑", preset.Name);
        Assert.Equal(Color.FromRgb(255, 204, 0), preset.Color);
        Assert.Equal(0.3, preset.Opacity);
        Assert.Equal(6.0, preset.Thickness);
    }

    [Fact]
    public void GetHighlighterPreset_2_초록()
    {
        var preset = ColorPresets.GetHighlighterPreset(2);
        Assert.Equal("초록", preset.Name);
        Assert.Equal(Color.FromRgb(52, 199, 89), preset.Color);
        Assert.Equal(0.3, preset.Opacity);
        Assert.Equal(6.0, preset.Thickness);
    }

    [Fact]
    public void GetHighlighterPreset_3_파랑()
    {
        var preset = ColorPresets.GetHighlighterPreset(3);
        Assert.Equal("파랑", preset.Name);
        Assert.Equal(Color.FromRgb(0, 122, 255), preset.Color);
        Assert.Equal(0.3, preset.Opacity);
        Assert.Equal(6.0, preset.Thickness);
    }

    // --- GetHighlighterPreset: 인덱스 순환 (modulo) ---

    [Fact]
    public void GetHighlighterPreset_인덱스순환_4는_0과동일()
    {
        var preset4 = ColorPresets.GetHighlighterPreset(4);
        var preset0 = ColorPresets.GetHighlighterPreset(0);
        Assert.Equal(preset0, preset4);
    }

    [Fact]
    public void GetHighlighterPreset_인덱스순환_5는_1과동일()
    {
        var preset5 = ColorPresets.GetHighlighterPreset(5);
        var preset1 = ColorPresets.GetHighlighterPreset(1);
        Assert.Equal(preset1, preset5);
    }

    [Fact]
    public void GetHighlighterPreset_큰인덱스_순환()
    {
        var preset100 = ColorPresets.GetHighlighterPreset(100);
        var preset0 = ColorPresets.GetHighlighterPreset(0);
        Assert.Equal(preset0, preset100); // 100 % 4 == 0
    }

    // --- GetThickness: 각 굵기 값 검증 ---

    [Fact]
    public void GetThickness_0_가는선()
    {
        Assert.Equal(3.0, ColorPresets.GetThickness(0));
    }

    [Fact]
    public void GetThickness_1_보통선()
    {
        Assert.Equal(6.0, ColorPresets.GetThickness(1));
    }

    [Fact]
    public void GetThickness_2_굵은선()
    {
        Assert.Equal(12.0, ColorPresets.GetThickness(2));
    }

    // --- GetThickness: 인덱스 순환 (modulo) ---

    [Fact]
    public void GetThickness_인덱스순환_3은_0과동일()
    {
        Assert.Equal(ColorPresets.GetThickness(0), ColorPresets.GetThickness(3));
    }

    [Fact]
    public void GetThickness_인덱스순환_4는_1과동일()
    {
        Assert.Equal(ColorPresets.GetThickness(1), ColorPresets.GetThickness(4));
    }

    [Fact]
    public void GetThickness_큰인덱스_순환()
    {
        Assert.Equal(ColorPresets.GetThickness(2), ColorPresets.GetThickness(99 * 3 + 2));
    }

    // --- GetHighlighterColor / GetHighlighterOpacity ---

    [Fact]
    public void GetHighlighterColor_프리셋Color와_일치()
    {
        for (int i = 0; i < ColorPresets.HighlighterColorCount; i++)
        {
            Assert.Equal(ColorPresets.GetHighlighterPreset(i).Color, ColorPresets.GetHighlighterColor(i));
        }
    }

    [Fact]
    public void GetHighlighterOpacity_프리셋Opacity와_일치()
    {
        for (int i = 0; i < ColorPresets.HighlighterColorCount; i++)
        {
            Assert.Equal(ColorPresets.GetHighlighterPreset(i).Opacity, ColorPresets.GetHighlighterOpacity(i));
        }
    }
}

using System.Windows.Media;

namespace MousePoint.Core;

public readonly record struct ToolPreset(string Name, Color Color, double Opacity, double Thickness);

public readonly record struct LaserPreset(string Name, Color MainColor, Color GlowColor);

/// <summary>
/// 색상/굵기 프리셋. 형광펜용 + 레이저용.
/// </summary>
public static class ColorPresets
{
    public static readonly Color LaserColor = Color.FromRgb(255, 0, 0);
    public const double LaserRadius = 6.0;
    public const double LaserTrailWidth = 3.0;

    private static readonly LaserPreset[] LaserPresets =
    [
        new("빨강", Color.FromRgb(255, 0, 0),   Color.FromRgb(255, 80, 80)),
        new("초록", Color.FromRgb(0, 220, 0),   Color.FromRgb(80, 255, 80)),
        new("파랑", Color.FromRgb(0, 100, 255), Color.FromRgb(80, 150, 255)),
        new("노랑", Color.FromRgb(255, 220, 0), Color.FromRgb(255, 240, 100)),
    ];

    private static readonly ToolPreset[] HighlighterPresets =
    [
        new("빨강", Color.FromRgb(255, 59, 48),  0.3, 6.0),
        new("노랑", Color.FromRgb(255, 204, 0),  0.3, 6.0),
        new("초록", Color.FromRgb(52, 199, 89),  0.3, 6.0),
        new("파랑", Color.FromRgb(0, 122, 255),  0.3, 6.0),
    ];

    private static readonly double[] ThicknessOptions = [3.0, 6.0, 12.0];

    public static int LaserColorCount => LaserPresets.Length;
    public static int HighlighterColorCount => HighlighterPresets.Length;
    public static int ThicknessCount => ThicknessOptions.Length;

    public static LaserPreset GetLaserPreset(int index) => LaserPresets[index % LaserPresets.Length];

    public static Color GetLaserColor(int index) => LaserPresets[index % LaserPresets.Length].MainColor;

    public static Color GetLaserGlowColor(int index) => LaserPresets[index % LaserPresets.Length].GlowColor;

    public static ToolPreset GetHighlighterPreset(int colorIndex)
    {
        return HighlighterPresets[colorIndex % HighlighterPresets.Length];
    }

    public static double GetThickness(int thicknessIndex)
    {
        return ThicknessOptions[thicknessIndex % ThicknessOptions.Length];
    }

    public static Color GetHighlighterColor(int colorIndex)
    {
        return HighlighterPresets[colorIndex % HighlighterPresets.Length].Color;
    }

    public static double GetHighlighterOpacity(int colorIndex)
    {
        return HighlighterPresets[colorIndex % HighlighterPresets.Length].Opacity;
    }
}

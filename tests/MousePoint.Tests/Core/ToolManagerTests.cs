using MousePoint.Core;
using Xunit;

namespace MousePoint.Tests.Core;

public class ToolManagerTests
{
    private readonly AppState _appState;
    private readonly ToolManager _manager;

    public ToolManagerTests()
    {
        _appState = new AppState();
        _manager = new ToolManager(_appState);
    }

    // --- 초기 상태 ---

    [Fact]
    public void 초기상태_비활성모드()
    {
        Assert.Equal(ToolMode.Inactive, _manager.CurrentMode);
    }

    [Fact]
    public void 초기상태_색상인덱스_0()
    {
        Assert.Equal(0, _manager.ColorIndex);
    }

    [Fact]
    public void 초기상태_굵기인덱스_0()
    {
        Assert.Equal(0, _manager.ThicknessIndex);
    }

    // --- 도구 전환 위임 ---

    [Fact]
    public void ToggleActivation_비활성에서_레이저로()
    {
        _manager.ToggleActivation();
        Assert.Equal(ToolMode.Laser, _manager.CurrentMode);
    }

    [Fact]
    public void ToggleActivation_레이저에서_비활성으로()
    {
        _manager.ToggleActivation(); // → Laser
        _manager.ToggleActivation(); // → Inactive
        Assert.Equal(ToolMode.Inactive, _manager.CurrentMode);
    }

    [Fact]
    public void CycleTool_비활성에서_레이저_형광펜_비활성()
    {
        _manager.CycleTool(); // → Laser
        Assert.Equal(ToolMode.Laser, _manager.CurrentMode);

        _manager.CycleTool(); // → Highlighter
        Assert.Equal(ToolMode.Highlighter, _manager.CurrentMode);

        _manager.CycleTool(); // → Inactive
        Assert.Equal(ToolMode.Inactive, _manager.CurrentMode);
    }

    [Fact]
    public void SetMode_직접설정()
    {
        _manager.SetMode(ToolMode.Highlighter);
        Assert.Equal(ToolMode.Highlighter, _manager.CurrentMode);
    }

    // --- CyclePreset: 형광펜 색상 순환 ---

    [Fact]
    public void CyclePreset_형광펜모드에서_색상순환()
    {
        _manager.SetMode(ToolMode.Highlighter);

        // 초기: 0 (빨강)
        Assert.Equal(0, _manager.ColorIndex);

        _manager.CyclePreset(); // → 1 (노랑)
        Assert.Equal(1, _manager.ColorIndex);

        _manager.CyclePreset(); // → 2 (초록)
        Assert.Equal(2, _manager.ColorIndex);

        _manager.CyclePreset(); // → 3 (파랑)
        Assert.Equal(3, _manager.ColorIndex);

        _manager.CyclePreset(); // → 0 (빨강, 순환)
        Assert.Equal(0, _manager.ColorIndex);
    }

    [Fact]
    public void CyclePreset_레이저모드에서_무시()
    {
        _manager.SetMode(ToolMode.Laser);

        _manager.CyclePreset();
        _manager.CyclePreset();

        // 색상 인덱스가 변하지 않음
        Assert.Equal(0, _manager.ColorIndex);
    }

    [Fact]
    public void CyclePreset_비활성모드에서_무시()
    {
        // 초기 비활성
        _manager.CyclePreset();
        _manager.CyclePreset();

        Assert.Equal(0, _manager.ColorIndex);
    }

    // --- PresetChanged 이벤트 ---

    [Fact]
    public void CyclePreset_이벤트발생()
    {
        _manager.SetMode(ToolMode.Highlighter);
        int firedCount = 0;
        int lastColor = -1;
        int lastThickness = -1;

        _manager.PresetChanged += (c, t) =>
        {
            firedCount++;
            lastColor = c;
            lastThickness = t;
        };

        _manager.CyclePreset(); // → 1

        Assert.Equal(1, firedCount);
        Assert.Equal(1, lastColor);
        Assert.Equal(0, lastThickness);
    }

    [Fact]
    public void CyclePreset_레이저모드에서_이벤트미발생()
    {
        _manager.SetMode(ToolMode.Laser);
        bool fired = false;
        _manager.PresetChanged += (_, _) => fired = true;

        _manager.CyclePreset();

        Assert.False(fired);
    }

    // --- SetColorIndex / SetThicknessIndex ---

    [Fact]
    public void SetColorIndex_유효값()
    {
        _manager.SetColorIndex(2);
        Assert.Equal(2, _manager.ColorIndex);
    }

    [Fact]
    public void SetColorIndex_범위초과시_순환()
    {
        // HighlighterColorCount = 4, 인덱스 5 → 5 % 4 = 1
        _manager.SetColorIndex(5);
        Assert.Equal(1, _manager.ColorIndex);
    }

    [Fact]
    public void SetColorIndex_음수시_예외()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _manager.SetColorIndex(-1));
    }

    [Fact]
    public void SetThicknessIndex_유효값()
    {
        _manager.SetThicknessIndex(1);
        Assert.Equal(1, _manager.ThicknessIndex);
    }

    [Fact]
    public void SetThicknessIndex_범위초과시_순환()
    {
        // ThicknessCount = 3, 인덱스 4 → 4 % 3 = 1
        _manager.SetThicknessIndex(4);
        Assert.Equal(1, _manager.ThicknessIndex);
    }

    [Fact]
    public void SetThicknessIndex_음수시_예외()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _manager.SetThicknessIndex(-1));
    }

    // --- 프리셋 값 조회 ---

    [Fact]
    public void GetCurrentHighlighterPreset_초기값()
    {
        var preset = _manager.GetCurrentHighlighterPreset();
        Assert.Equal("빨강", preset.Name);
    }

    [Fact]
    public void GetCurrentHighlighterPreset_순환후()
    {
        _manager.SetMode(ToolMode.Highlighter);
        _manager.CyclePreset(); // → 1 (노랑)

        var preset = _manager.GetCurrentHighlighterPreset();
        Assert.Equal("노랑", preset.Name);
    }

    [Fact]
    public void GetCurrentThickness_초기값()
    {
        double thickness = _manager.GetCurrentThickness();
        Assert.Equal(3.0, thickness);
    }

    [Fact]
    public void GetCurrentThickness_변경후()
    {
        _manager.SetThicknessIndex(2);
        double thickness = _manager.GetCurrentThickness();
        Assert.Equal(12.0, thickness);
    }

    // --- null AppState 방어 ---

    [Fact]
    public void 생성자_null_AppState_예외()
    {
        Assert.Throws<ArgumentNullException>(() => new ToolManager(null!));
    }

    // --- PresetChanged 이벤트: SetColorIndex, SetThicknessIndex ---

    [Fact]
    public void SetColorIndex_이벤트발생()
    {
        bool fired = false;
        _manager.PresetChanged += (_, _) => fired = true;

        _manager.SetColorIndex(2);

        Assert.True(fired);
    }

    [Fact]
    public void SetThicknessIndex_이벤트발생()
    {
        int lastThickness = -1;
        _manager.PresetChanged += (_, t) => lastThickness = t;

        _manager.SetThicknessIndex(1);

        Assert.Equal(1, lastThickness);
    }

    // --- 굵기 인덱스는 CyclePreset에서 변하지 않음 ---

    [Fact]
    public void CyclePreset_굵기인덱스_불변()
    {
        _manager.SetMode(ToolMode.Highlighter);
        _manager.SetThicknessIndex(2);

        _manager.CyclePreset();

        Assert.Equal(2, _manager.ThicknessIndex);
    }
}

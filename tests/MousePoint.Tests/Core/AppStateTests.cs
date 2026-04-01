using MousePoint.Core;
using Xunit;

namespace MousePoint.Tests.Core;

public class AppStateTests
{
    private readonly AppState _state;

    public AppStateTests()
    {
        _state = new AppState();
    }

    // --- 초기 상태 ---

    [Fact]
    public void 초기상태_비활성()
    {
        Assert.Equal(ToolMode.Inactive, _state.CurrentMode);
    }

    // --- ToggleActivation ---

    [Fact]
    public void ToggleActivation_비활성에서_레이저로()
    {
        _state.ToggleActivation();
        Assert.Equal(ToolMode.Laser, _state.CurrentMode);
    }

    [Fact]
    public void ToggleActivation_레이저에서_비활성으로()
    {
        _state.ToggleActivation(); // → Laser
        _state.ToggleActivation(); // → Inactive
        Assert.Equal(ToolMode.Inactive, _state.CurrentMode);
    }

    [Fact]
    public void ToggleActivation_형광펜에서_비활성으로()
    {
        _state.SetMode(ToolMode.Highlighter);
        _state.ToggleActivation(); // Highlighter → Inactive
        Assert.Equal(ToolMode.Inactive, _state.CurrentMode);
    }

    [Fact]
    public void ToggleActivation_순환_Inactive_Laser_Inactive()
    {
        Assert.Equal(ToolMode.Inactive, _state.CurrentMode);
        _state.ToggleActivation();
        Assert.Equal(ToolMode.Laser, _state.CurrentMode);
        _state.ToggleActivation();
        Assert.Equal(ToolMode.Inactive, _state.CurrentMode);
    }

    // --- CycleTool ---

    [Fact]
    public void CycleTool_비활성에서_레이저로()
    {
        _state.CycleTool();
        Assert.Equal(ToolMode.Laser, _state.CurrentMode);
    }

    [Fact]
    public void CycleTool_레이저에서_형광펜으로()
    {
        _state.CycleTool(); // → Laser
        _state.CycleTool(); // → Highlighter
        Assert.Equal(ToolMode.Highlighter, _state.CurrentMode);
    }

    [Fact]
    public void CycleTool_형광펜에서_비활성으로()
    {
        _state.CycleTool(); // → Laser
        _state.CycleTool(); // → Highlighter
        _state.CycleTool(); // → Inactive
        Assert.Equal(ToolMode.Inactive, _state.CurrentMode);
    }

    [Fact]
    public void CycleTool_전체순환_Inactive_Laser_Highlighter_Inactive()
    {
        Assert.Equal(ToolMode.Inactive, _state.CurrentMode);

        _state.CycleTool();
        Assert.Equal(ToolMode.Laser, _state.CurrentMode);

        _state.CycleTool();
        Assert.Equal(ToolMode.Highlighter, _state.CurrentMode);

        _state.CycleTool();
        Assert.Equal(ToolMode.Inactive, _state.CurrentMode);
    }

    // --- SetMode ---

    [Fact]
    public void SetMode_레이저_직접설정()
    {
        _state.SetMode(ToolMode.Laser);
        Assert.Equal(ToolMode.Laser, _state.CurrentMode);
    }

    [Fact]
    public void SetMode_형광펜_직접설정()
    {
        _state.SetMode(ToolMode.Highlighter);
        Assert.Equal(ToolMode.Highlighter, _state.CurrentMode);
    }

    [Fact]
    public void SetMode_비활성_직접설정()
    {
        _state.SetMode(ToolMode.Laser); // 먼저 Laser로
        _state.SetMode(ToolMode.Inactive);
        Assert.Equal(ToolMode.Inactive, _state.CurrentMode);
    }

    // --- ModeChanged 이벤트 ---

    [Fact]
    public void ToggleActivation_ModeChanged_이벤트발생()
    {
        ToolMode? capturedOld = null;
        ToolMode? capturedNew = null;
        _state.ModeChanged += (old, @new) =>
        {
            capturedOld = old;
            capturedNew = @new;
        };

        _state.ToggleActivation();

        Assert.Equal(ToolMode.Inactive, capturedOld);
        Assert.Equal(ToolMode.Laser, capturedNew);
    }

    [Fact]
    public void CycleTool_ModeChanged_이벤트발생()
    {
        int firedCount = 0;
        _state.ModeChanged += (_, _) => firedCount++;

        _state.CycleTool(); // Inactive → Laser
        _state.CycleTool(); // Laser → Highlighter
        _state.CycleTool(); // Highlighter → Inactive

        Assert.Equal(3, firedCount);
    }

    [Fact]
    public void SetMode_ModeChanged_이벤트발생()
    {
        ToolMode? capturedOld = null;
        ToolMode? capturedNew = null;
        _state.ModeChanged += (old, @new) =>
        {
            capturedOld = old;
            capturedNew = @new;
        };

        _state.SetMode(ToolMode.Highlighter);

        Assert.Equal(ToolMode.Inactive, capturedOld);
        Assert.Equal(ToolMode.Highlighter, capturedNew);
    }

    [Fact]
    public void SetMode_같은모드_이벤트미발생()
    {
        bool fired = false;
        _state.ModeChanged += (_, _) => fired = true;

        // 초기상태가 Inactive이므로 Inactive로 SetMode → 변경 없음
        _state.SetMode(ToolMode.Inactive);

        Assert.False(fired);
    }

    [Fact]
    public void SetMode_같은모드_연속호출_이벤트미발생()
    {
        _state.SetMode(ToolMode.Laser);

        int firedCount = 0;
        _state.ModeChanged += (_, _) => firedCount++;

        _state.SetMode(ToolMode.Laser); // 같은 모드
        _state.SetMode(ToolMode.Laser); // 같은 모드

        Assert.Equal(0, firedCount);
    }

    [Fact]
    public void ModeChanged_이벤트_올바른_oldMode_newMode_전달()
    {
        var transitions = new List<(ToolMode old, ToolMode @new)>();
        _state.ModeChanged += (old, @new) => transitions.Add((old, @new));

        _state.CycleTool(); // Inactive → Laser
        _state.CycleTool(); // Laser → Highlighter
        _state.CycleTool(); // Highlighter → Inactive

        Assert.Equal(3, transitions.Count);
        Assert.Equal((ToolMode.Inactive, ToolMode.Laser), transitions[0]);
        Assert.Equal((ToolMode.Laser, ToolMode.Highlighter), transitions[1]);
        Assert.Equal((ToolMode.Highlighter, ToolMode.Inactive), transitions[2]);
    }
}

namespace MousePoint.Core;

public enum ToolMode
{
    Inactive,
    Laser,
    Highlighter
}

/// <summary>
/// 3개 상태 머신: 비활성 / 레이저 / 형광펜.
/// 순수 로직 — UI/렌더링 의존성 없음, 단위 테스트 가능.
/// </summary>
public sealed class AppState
{
    private ToolMode _currentMode = ToolMode.Inactive;

    public ToolMode CurrentMode => _currentMode;

    public event Action<ToolMode, ToolMode>? ModeChanged; // (oldMode, newMode)

    /// <summary>
    /// F9: 비활성 ↔ 활성(레이저) 토글.
    /// 형광펜 상태에서도 비활성으로 전환.
    /// </summary>
    public void ToggleActivation()
    {
        var oldMode = _currentMode;
        _currentMode = _currentMode == ToolMode.Inactive
            ? ToolMode.Laser
            : ToolMode.Inactive;

        if (oldMode != _currentMode)
            ModeChanged?.Invoke(oldMode, _currentMode);
    }

    /// <summary>
    /// XBUTTON1: 도구 순환 (비활성→레이저→형광펜→비활성).
    /// </summary>
    public void CycleTool()
    {
        var oldMode = _currentMode;
        _currentMode = _currentMode switch
        {
            ToolMode.Inactive => ToolMode.Laser,
            ToolMode.Laser => ToolMode.Highlighter,
            ToolMode.Highlighter => ToolMode.Inactive,
            _ => ToolMode.Inactive
        };

        if (oldMode != _currentMode)
            ModeChanged?.Invoke(oldMode, _currentMode);
    }

    /// <summary>
    /// 직접 모드 설정 (키보드 폴백용: Ctrl+Shift+1/2/3).
    /// </summary>
    public void SetMode(ToolMode mode)
    {
        var oldMode = _currentMode;
        _currentMode = mode;

        if (oldMode != _currentMode)
            ModeChanged?.Invoke(oldMode, _currentMode);
    }
}

namespace MousePoint.Core;

public enum ToolMode
{
    Inactive,
    Laser,        // 레이저 포인터(색상/렌더 인프라). 단독 진입 없음 — 형광포인터에 통합됨.
    Highlighter,  // 형광포인터: 레이저 포인터 + 좌클릭 드래그 형광펜 통합 모드.
    Rectangle
}

/// <summary>
/// 상태 머신: 비활성 / 형광포인터 / 네모박스.
/// 순수 로직 — UI/렌더링 의존성 없음, 단위 테스트 가능.
/// </summary>
public sealed class AppState
{
    private ToolMode _currentMode = ToolMode.Inactive;

    public ToolMode CurrentMode => _currentMode;

    public event Action<ToolMode, ToolMode>? ModeChanged; // (oldMode, newMode)

    /// <summary>
    /// F9: 비활성 ↔ 활성(형광포인터) 토글.
    /// </summary>
    public void ToggleActivation()
    {
        var oldMode = _currentMode;
        _currentMode = _currentMode == ToolMode.Inactive
            ? ToolMode.Highlighter
            : ToolMode.Inactive;

        if (oldMode != _currentMode)
            ModeChanged?.Invoke(oldMode, _currentMode);
    }

    /// <summary>
    /// XBUTTON1: 도구 순환 (비활성→형광포인터→네모박스→비활성).
    /// </summary>
    public void CycleTool()
    {
        var oldMode = _currentMode;
        _currentMode = _currentMode switch
        {
            ToolMode.Inactive => ToolMode.Highlighter,
            ToolMode.Highlighter => ToolMode.Rectangle,
            ToolMode.Rectangle => ToolMode.Inactive,
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

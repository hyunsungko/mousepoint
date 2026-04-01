namespace MousePoint.Core;

/// <summary>
/// AppState를 감싸서 도구 전환 + 색상/굵기 순환을 관리하는 중앙 컨트롤러.
/// 순수 로직 — UI 의존성 없음, 단위 테스트 가능.
/// </summary>
public sealed class ToolManager
{
    private readonly AppState _appState;

    private int _colorIndex;
    private int _thicknessIndex;
    private int _laserColorIndex;

    public AppState AppState => _appState;

    /// <summary>현재 형광펜 색상 인덱스 (0-based).</summary>
    public int ColorIndex => _colorIndex;

    /// <summary>현재 굵기 인덱스 (0-based).</summary>
    public int ThicknessIndex => _thicknessIndex;

    /// <summary>현재 레이저 색상 인덱스 (0-based).</summary>
    public int LaserColorIndex => _laserColorIndex;

    /// <summary>현재 도구 모드 (AppState 위임).</summary>
    public ToolMode CurrentMode => _appState.CurrentMode;

    /// <summary>
    /// 형광펜 프리셋이 변경될 때 발생. (colorIndex, thicknessIndex)
    /// </summary>
    public event Action<int, int>? PresetChanged;

    /// <summary>
    /// 레이저 프리셋이 변경될 때 발생. (laserColorIndex)
    /// </summary>
    public event Action<int>? LaserPresetChanged;

    public ToolManager(AppState appState)
    {
        _appState = appState ?? throw new ArgumentNullException(nameof(appState));
    }

    /// <summary>
    /// F9: 활성/비활성 토글 (AppState에 위임).
    /// </summary>
    public void ToggleActivation()
    {
        _appState.ToggleActivation();
    }

    /// <summary>
    /// XBUTTON1: 도구 순환 (AppState에 위임).
    /// </summary>
    public void CycleTool()
    {
        _appState.CycleTool();
    }

    /// <summary>
    /// 직접 모드 설정 (AppState에 위임).
    /// </summary>
    public void SetMode(ToolMode mode)
    {
        _appState.SetMode(mode);
    }

    /// <summary>
    /// XBUTTON2: 현재 도구의 프리셋 순환.
    /// - 형광펜: 색상 순환 (빨→노→초→파→빨)
    /// - 레이저: 색상 순환 (빨→초→파→노→빨)
    /// - 비활성: 무시
    /// </summary>
    public void CyclePreset()
    {
        switch (_appState.CurrentMode)
        {
            case ToolMode.Highlighter:
                _colorIndex = (_colorIndex + 1) % ColorPresets.HighlighterColorCount;
                PresetChanged?.Invoke(_colorIndex, _thicknessIndex);
                break;
            case ToolMode.Laser:
                _laserColorIndex = (_laserColorIndex + 1) % ColorPresets.LaserColorCount;
                LaserPresetChanged?.Invoke(_laserColorIndex);
                break;
        }
    }

    /// <summary>
    /// 마우스 휠: 형광펜 굵기 순환.
    /// </summary>
    public void CycleThickness(bool up)
    {
        if (_appState.CurrentMode != ToolMode.Highlighter) return;
        _thicknessIndex = up
            ? (_thicknessIndex + 1) % ColorPresets.ThicknessCount
            : (_thicknessIndex - 1 + ColorPresets.ThicknessCount) % ColorPresets.ThicknessCount;
        PresetChanged?.Invoke(_colorIndex, _thicknessIndex);
    }

    /// <summary>
    /// 색상 인덱스를 직접 설정 (외부에서 초기화 또는 복원 시 사용).
    /// </summary>
    public void SetColorIndex(int index)
    {
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));

        _colorIndex = index % ColorPresets.HighlighterColorCount;
        PresetChanged?.Invoke(_colorIndex, _thicknessIndex);
    }

    /// <summary>
    /// 굵기 인덱스를 직접 설정.
    /// </summary>
    public void SetThicknessIndex(int index)
    {
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));

        _thicknessIndex = index % ColorPresets.ThicknessCount;
        PresetChanged?.Invoke(_colorIndex, _thicknessIndex);
    }

    /// <summary>
    /// 레이저 색상 인덱스를 직접 설정.
    /// </summary>
    public void SetLaserColorIndex(int index)
    {
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));

        _laserColorIndex = index % ColorPresets.LaserColorCount;
        LaserPresetChanged?.Invoke(_laserColorIndex);
    }

    /// <summary>
    /// 현재 형광펜 프리셋을 가져온다.
    /// </summary>
    public ToolPreset GetCurrentHighlighterPreset()
    {
        return ColorPresets.GetHighlighterPreset(_colorIndex);
    }

    /// <summary>
    /// 현재 레이저 프리셋을 가져온다.
    /// </summary>
    public LaserPreset GetCurrentLaserPreset()
    {
        return ColorPresets.GetLaserPreset(_laserColorIndex);
    }

    /// <summary>
    /// 현재 굵기 값을 가져온다.
    /// </summary>
    public double GetCurrentThickness()
    {
        return ColorPresets.GetThickness(_thicknessIndex);
    }
}

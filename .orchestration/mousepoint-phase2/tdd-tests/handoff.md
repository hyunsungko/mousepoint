테스트가 WSL (Linux) 환경에서 실행 불가한 상태입니다 — `Microsoft.WindowsDesktop.App` 런타임이 Linux에 없기 때문입니다. WPF 프로젝트의 알려진 제약입니다.

빌드는 성공하므로 코드 정합성은 확인되었습니다. 작성된 내용을 정리합니다.

## 결과 요약

### 1. `tests/MousePoint.Tests/Core/AppStateTests.cs` — 기존 파일, 이미 완전
- 13개 테스트 메서드
- ToggleActivation 순환 (Inactive↔Laser, Highlighter→Inactive)
- CycleTool 전체 순환 (Inactive→Laser→Highlighter→Inactive)
- SetMode 직접 설정 (각 모드)
- ModeChanged 이벤트 발생/미발생 확인
- 같은 모드 SetMode 시 이벤트 미발생 확인
- old/new 모드 전달값 검증

### 2. `tests/MousePoint.Tests/Core/ColorPresetsTests.cs` — 신규 생성
- 19개 테스트 메서드
- HighlighterColorCount == 4 검증
- ThicknessCount == 3 검증
- 레이저 상수값 검증 (Color, Radius, TrailWidth)
- 4개 형광펜 프리셋 각각의 Name, Color, Opacity, Thickness 검증
- GetHighlighterPreset 인덱스 순환(modulo) 동작 검증 (4→0, 5→1, 100→0)
- GetThickness 인덱스 순환(modulo) 동작 검증 (3→0, 4→1)
- GetHighlighterColor/GetHighlighterOpacity와 프리셋 일치 검증

### 3. `tests/MousePoint.Tests/Core/ToolManagerTests.cs` — 기존 파일, 이미 완전
- 22개 테스트 메서드
- CyclePreset: 형광펜 모드에서 색상 순환 (0→1→2→3→0)
- CyclePreset: 레이저/비활성 모드에서 무시
- PresetChanged 이벤트 발생/미발생 확인
- SetColorIndex/SetThicknessIndex 유효값, 순환, 음수 예외
- 프리셋값 조회 및 null AppState 방어

### 빌드 상태
- `dotnet build` — **성공** (0 errors, 0 warnings)
- `dotnet test` — WSL 환경에서 `Microsoft.WindowsDesktop.App` 런타임 부재로 실행 불가 (Windows에서 실행 필요)

**총 테스트 수: 54개** (AppState 13 + ColorPresets 19 + ToolManager 22)

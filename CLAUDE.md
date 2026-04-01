# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

MousePoint — Windows 강의/프레젠테이션용 화면 오버레이 주석 도구. C#/.NET 8 + WPF.
전체 화면 투명 오버레이 위에 레이저 포인터와 형광펜 주석을 그린다.

## Build & Test

```bash
# 빌드 (WSL에서도 가능 — Directory.Build.props에 EnableWindowsTargeting 설정됨)
dotnet build MousePoint.sln

# 테스트 (xUnit)
dotnet test MousePoint.sln

# 단일 테스트 실행
dotnet test tests/MousePoint.Tests --filter "FullyQualifiedName~ToolManagerTests.CyclePreset"

# Release 빌드 + 단일 실행파일 퍼블리시
dotnet publish src/MousePoint/MousePoint.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish
```

## Architecture

### 상태 머신 (Core)
- `AppState`: 3-상태 FSM (`Inactive → Laser → Highlighter`). 이벤트 기반, UI 의존성 없음.
- `ToolManager`: AppState를 감싸고 색상/굵기 프리셋 순환을 관리. 순수 로직 — 테스트 가능.
- `ColorPresets`: 레이저/형광펜의 색상·투명도·굵기 상수 정의.

### 입력 (Input)
- `GlobalMouseHook`: WH_MOUSE_LL 로우레벨 훅. WPF Dispatcher로 마샬링.
- `GlobalKeyboardHook`: RegisterHotKey 기반 (F9 토글, Ctrl+Shift+1/2/3 직접 모드).
- `NativeMethods`: Win32 P/Invoke 선언 모음.

### 렌더링 (Rendering)
- 2-레이어 Canvas 구조: `OverlayCanvas`(완성 스트로크/레이저) + `ActiveCanvas`(드래그 중 형광펜).
- `LaserRenderer`: 링 버퍼 + CompositionTarget.Rendering 프레임 렌더. Line/Ellipse 객체 풀링.
- `HighlighterRenderer`: PathGeometry 기반 반투명 스트로크. 최소 거리 필터링(2px). 완료 시 FadeOutManager에 등록.
- `FadeOutManager`: 완성된 스트로크를 3초 후 opacity 애니메이션으로 제거.
- `ModeIndicator`: 모드 전환 시 커서 근처에 일시적 인디케이터 표시.

### 핵심 패턴
- **WS_EX_TRANSPARENT 토글**: 형광펜 모드에서만 클릭 통과 해제 (드래그 입력 필요), 나머지는 클릭 통과.
- **DPI 보정**: HwndSource.CompositionTarget.TransformFromDevice로 물리 픽셀 → WPF DIU 변환. 모든 마우스 좌표는 `ScreenToCanvas()`를 거침.
- **단일 인스턴스**: Global Mutex로 중복 실행 방지.
- **트레이 아이콘**: WinForms NotifyIcon 사용 (WPF+WinForms 혼합). 네임스페이스 충돌 방지를 위해 csproj에서 System.Drawing/System.Windows.Forms Using 제거됨.

## Skill routing

When the user's request matches an available skill, ALWAYS invoke it using the Skill
tool as your FIRST action. Do NOT answer directly, do NOT use other tools first.
The skill has specialized workflows that produce better results than ad-hoc answers.

Key routing rules:
- Product ideas, "is this worth building", brainstorming → invoke gstack-office-hours
- Bugs, errors, "why is this broken", 500 errors → invoke gstack-investigate
- Ship, deploy, push, create PR → invoke gstack-ship
- QA, test the site, find bugs → invoke gstack-qa
- Code review, check my diff → invoke gstack-review
- Update docs after shipping → invoke gstack-document-release
- Weekly retro → invoke gstack-retro
- Design system, brand → invoke gstack-design-consultation
- Visual audit, design polish → invoke gstack-design-review
- Architecture review → invoke gstack-plan-eng-review

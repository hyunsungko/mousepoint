# MousePoint

Windows 강의/프레젠테이션용 화면 오버레이 주석 도구.

마우스를 따라다니는 레이저 포인터와 형광펜으로 화면 위에 직접 그릴 수 있습니다. 모든 앱 위에서 동작하며, PPT 전체화면, Zoom 화면공유, 멀티모니터 환경을 지원합니다.

## 주요 기능

- **레이저 포인터** - 빨간 원 + fade-out trail. 마우스를 따라 부드럽게 이동
- **형광펜** - 반투명 스트로크로 화면 위에 직접 그리기. 4색(빨강/노랑/초록/파랑), 3단계 굵기
- **자동 fade-out** - 형광펜 스트로크가 3초 후 자동으로 사라짐
- **마우스 사이드 버튼** - 사이드 버튼 1로 도구 전환, 사이드 버튼 2로 색상 순환
- **키보드 단축키** - F9 활성화/비활성화, Ctrl+Shift+1/2/3 도구 직접 선택
- **시스템 트레이** - 백그라운드 실행, 우클릭 메뉴로 제어
- **멀티모니터** - 모든 모니터에 걸쳐 동작
- **첫 실행 가이드** - 처음 실행 시 사용법 안내 오버레이

## 설치

[Releases](https://github.com/hyunsungko/mousepoint/releases) 에서 `MousePoint.exe`를 다운로드하여 실행합니다. 설치 불필요 (portable single-file).

### 요구사항

- Windows 10/11 (x64)
- .NET 8 런타임 포함 (self-contained)

## 사용법

### 기본 조작

| 입력 | 동작 |
|------|------|
| F9 | 활성화 / 비활성화 |
| 마우스 사이드 버튼 1 | 도구 전환 (레이저 → 형광펜 → 비활성) |
| 마우스 사이드 버튼 2 | 색상/굵기 순환 |
| 좌클릭 + 드래그 | 형광펜 모드에서 그리기 |
| Ctrl+Shift+1 | 레이저 포인터 |
| Ctrl+Shift+2 | 형광펜 |
| Ctrl+Shift+3 | 비활성 |

### 상태 전환

```
비활성 ←→ 레이저 ←→ 형광펜
        (F9)    (사이드 버튼 1)
```

## 빌드

```bash
dotnet build MousePoint.sln
```

### 배포용 빌드 (single-file)

```bash
dotnet publish src/MousePoint/MousePoint.csproj -r win-x64 --self-contained -p:PublishSingleFile=true -o ./publish
```

### 테스트

```bash
dotnet test tests/MousePoint.Tests/
```

## 기술 스택

- .NET 8 + WPF (C#)
- Win32 Interop (SetWindowsHookEx, WS_EX_TRANSPARENT, RegisterHotKey)
- CompositionTarget.Rendering (vsync 동기 렌더링)
- PathGeometry 기반 형광펜 (InkCanvas 미사용)

## 프로젝트 구조

```
src/MousePoint/
  Core/         상태 머신, 도구 순환, 색상 프리셋
  Input/        글로벌 마우스 훅, 키보드 핫키, P/Invoke
  Rendering/    레이저 포인터, 형광펜, fade-out, 모드 인디케이터
  UI/           시스템 트레이, 첫 실행 온보딩
tests/MousePoint.Tests/
  Core/         AppState, ToolManager, ColorPresets 단위 테스트 (54개)
```

## 알려진 제한사항

- 전체화면 DirectX 앱(게임 등) 위에서는 오버레이가 표시되지 않을 수 있음
- 코드 서명이 없어 Windows SmartScreen/Smart App Control 경고 발생
- 형광펜을 빠르게 연속으로 그리면 버벅임 발생 ([#001](docs/issues/001-highlighter-stutter.md))

## 라이선스

MIT

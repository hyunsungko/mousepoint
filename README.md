# MousePoint

Windows 강의/프레젠테이션용 화면 오버레이 주석 도구.

마우스를 따라다니는 레이저 포인터, 형광펜, 네모박스로 화면 위에 직접 그릴 수 있습니다. 모든 앱 위에서 동작하며, PPT 전체화면, Zoom 화면공유, 멀티모니터 환경을 지원합니다.

## 주요 기능

- **레이저 포인터** - 4색(빨강/초록/파랑/노랑) + 발광 효과 + 그라데이션 trail
- **형광펜** - 반투명 스트로크. 4색, 3단계 굵기(스크롤 휠), 자동 fade-out
- **네모박스** - 드래그로 반투명 형광색 사각형. 3단계 테두리 굵기(스크롤 휠), 자동 fade-out
- **자동 fade-out** - 모든 주석이 일정 시간 후 자동으로 사라짐
- **마우스 사이드 버튼** - 사이드 버튼 1로 도구 전환, 사이드 버튼 2로 색상 순환
- **DWM 하드웨어 가속** - GPU 렌더링으로 빠른 연속 드로잉에서도 부드러운 성능
- **멀티모니터** - 모든 모니터에 걸쳐 동작
- **첫 실행 가이드** - 처음 실행 시 사용법 안내 오버레이 (한국어/영어)

## 설치

[Releases](https://github.com/hyunsungko/mousepoint/releases) 에서 `MousePoint.exe`를 다운로드하여 실행합니다. 설치 불필요 (portable single-file).

### 요구사항

- Windows 10/11 (x64)
- .NET 8 런타임 포함 (self-contained)

### 처음 실행 시 SmartScreen 경고

코드 서명이 없는 오픈소스 프로그램이라 "Windows가 PC를 보호했습니다" 경고가 나타날 수 있습니다. **추가 정보 → 실행**을 클릭하세요. 1회만 필요합니다.

## 사용법

### 기본 조작

| 입력 | 동작 |
|------|------|
| F9 | 오버레이 ON/OFF |
| ESC | 오버레이 비활성화 (온보딩 중: 앱 종료) |
| 마우스 사이드 버튼 1 | 도구 순환 (레이저 → 형광펜 → 네모박스 → 비활성) |
| 마우스 사이드 버튼 2 | 색상 순환 |
| 스크롤 휠 | 형광펜 굵기 / 네모박스 테두리 굵기 조절 |
| 좌클릭 + 드래그 | 형광펜 그리기 / 네모박스 그리기 |
| Ctrl+Shift+1 | 레이저 포인터 |
| Ctrl+Shift+2 | 형광펜 |
| Ctrl+Shift+3 | 비활성 |
| Ctrl+Shift+4 | 네모박스 |
| Ctrl+Shift+Q | 종료 |

### 상태 전환

```
[비활성] ←(F9/ESC)→ [레이저] ←(사이드1)→ [형광펜] ←(사이드1)→ [네모박스] ←(사이드1)→ [비활성]
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
- DWM (Desktop Window Manager) 하드웨어 가속 투명도
- Win32 Interop (SetWindowsHookEx, RegisterHotKey, DwmExtendFrameIntoClientArea)
- CompositionTarget.Rendering (vsync 동기 렌더링)
- PathGeometry 기반 형광펜 + Douglas-Peucker 포인트 간소화

## 프로젝트 구조

```
src/MousePoint/
  Core/         상태 머신, 도구 순환, 색상 프리셋, Douglas-Peucker
  Input/        글로벌 마우스 훅, 키보드 핫키, P/Invoke
  Rendering/    레이저, 형광펜, 네모박스, fade-out, 모드 인디케이터
  UI/           시스템 트레이, 첫 실행 온보딩
tests/MousePoint.Tests/
  Core/         AppState, ToolManager, ColorPresets, DouglasPeucker 단위 테스트
```

## 알려진 제한사항

- 전체화면 DirectX 앱(게임 등) 위에서는 오버레이가 표시되지 않을 수 있음
- 코드 서명이 없어 Windows SmartScreen 경고 발생 (1회 "추가 정보 → 실행" 필요)
- 오버레이 활성 시 마우스 클릭이 아래 앱으로 전달되지 않음 (F9/ESC로 비활성화)

## 라이선스

MIT

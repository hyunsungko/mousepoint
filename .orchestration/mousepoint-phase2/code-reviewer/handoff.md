---

## MousePoint 코드 리뷰 결과

### 수정 완료 (6건)

| # | 심각도 | 파일 | 이슈 | 수정 내용 |
|---|--------|------|------|-----------|
| 1 | **Critical** | `LaserRenderer.cs` | `_canvas.Children.Clear()` 매 프레임 호출 → 형광펜 스트로크 삭제 | 자신이 관리하는 요소만 제거/추가하도록 변경 |
| 2 | **Critical** | `LaserRenderer.cs` | 매 프레임 60개 `Line` + `SolidColorBrush` 객체 생성 (60fps × 60개 = 3600obj/s) | `Line[]` 풀 사전 할당, 좌표/opacity만 갱신 |
| 3 | **Important** | `LaserRenderer.cs`, `FadeOutManager.cs` | `CompositionTarget.Rendering` 구독 해제 보장 없음 | `IDisposable` 구현 |
| 4 | **Important** | `MainWindow.xaml.cs` | `OnClosed`에서 `ModeChanged`, `PresetChanged` 이벤트 구독 해제 누락 | 이벤트 해제 + 렌더러 Dispose 추가 |
| 5 | **Important** | `App.xaml.cs` | `ReleaseMutex()`가 비정상 종료 시 `ApplicationException` 발생 가능 | try-catch 추가 |
| 6 | **Important** | `GlobalMouseHook.cs`, `MainWindow.xaml.cs` | `MainModule!` null-forgiving + `PresentationSource` 무조건 캐스트 | null 안전 패턴으로 변경 |

### 수정 불가능한 구조적 이슈 (handoff 기록용)

1. **MVVM 패턴 미사용**: 현재 code-behind 방식. MVP 단계에서는 수용 가능하나, 설정 화면 추가 시 ViewModel 도입 권장.
2. **링 버퍼에서 timestamp 미사용**: 원래 `(x, y, DateTime)` 튜플이었으나 timestamp가 실제 로직에서 사용되지 않음 → 제거함. 향후 시간 기반 trail decay가 필요하면 재도입.
3. **HighlighterRenderer의 PathGeometry 누적**: 긴 드래그 시 `LineSegment`가 수천 개 누적될 수 있음. 포인트 간소화(Douglas-Peucker) 또는 세그먼트 상한선 고려.
4. **멀티 모니터 DPI 차이**: `SystemParameters.VirtualScreen*` 기반 좌표 변환은 동일 DPI 가정. Per-monitor DPI 환경에서 좌표 어긋남 가능.

### 양호한 부분

- **Core 계층 (AppState, ToolManager, ColorPresets)**: UI 의존성 없는 순수 로직, 단위 테스트 가능. 상태 머신이 깔끔.
- **관심사 분리**: Input/Core/Rendering/UI 네임스페이스 구분이 적절.
- **마우스 훅 watchdog**: 훅 유실 시 자동 재설치 — 실전에서 중요한 방어 로직.
- **OnboardingOverlay**: 이벤트 구독/해제가 올바르게 쌍으로 처리됨.
- **단일 인스턴스 보장**: `Mutex` 기반 중복 실행 방지 적절.

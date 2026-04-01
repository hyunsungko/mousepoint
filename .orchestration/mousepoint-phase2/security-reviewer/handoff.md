## MousePoint 보안 리뷰 완료

### 수정한 보안 이슈 (4건)

| # | 심각도 | 파일 | 이슈 | 수정 |
|---|--------|------|------|------|
| 1 | **높음** | `GlobalMouseHook.cs:83` | HookCallback에서 예외 발생 시 `CallNextHookEx` 미호출 → 시스템 전체 마우스 훅 체인 파손 | try-catch로 감싸서 `CallNextHookEx`가 항상 호출되도록 보장 |
| 2 | **중간** | `NativeMethods.cs:61-64` | `GetWindowLong`/`SetWindowLong`은 32비트 전용, x64에서 포인터 값 잘림 가능 | `GetWindowLongPtrW`/`SetWindowLongPtrW`로 교체 |
| 3 | **중간** | `App.xaml.cs:25-28` | 단일 인스턴스 체크 실패 시 소유하지 않은 Mutex에 `ReleaseMutex()` 호출 → `ApplicationException` | `_ownsMutex` 플래그로 소유 여부 추적 |
| 4 | **낮음** | `GlobalMouseHook.cs:44` | `SetWindowsHookEx` 실패 시 에러 코드가 무시됨 | `Marshal.GetLastWin32Error()` 로깅 추가 |

### 이슈 없음으로 확인된 항목

- **P/Invoke 구조체 레이아웃**: `MSLLHOOKSTRUCT`, `POINT` 모두 `LayoutKind.Sequential` 올바르게 적용
- **CallNextHookEx 호출**: 항상 호출됨 (수정 후)
- **훅 핸들 유출**: `Dispose()`에서 `Uninstall()` 호출, watchdog에서도 `Uninstall()` 후 재설치
- **경로 주입**: `OnboardingOverlay`의 마커 파일 경로는 `Environment.SpecialFolder.LocalApplicationData` + 하드코딩 경로로 주입 불가
- **WS_EX_TRANSPARENT 토글**: `HwndSource` null 체크 존재
- **IDisposable 패턴**: `GlobalMouseHook`, `GlobalKeyboardHook`, `TrayIconManager` 모두 올바르게 구현
- **이벤트 핸들러 해제**: `OnClosed`에서 Dispose 호출, `OnboardingOverlay`에서 이벤트 해제 후 제거
- **NotifyIcon 정리**: `Dispose()`에서 `Visible = false` 후 `Dispose()` 호출

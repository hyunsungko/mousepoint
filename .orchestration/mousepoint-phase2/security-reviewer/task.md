# Worker Task: security-reviewer

- Session: `mousepoint-phase2`
- Repo root: `/home/ffgtt/projects/mousepoint-dev`
- Worktree: `/home/ffgtt/projects/mousepoint-dev-mousepoint-phase2-security-reviewer`
- Branch: `orchestrator-mousepoint-phase2-security-reviewer`
- Launcher status file: `/home/ffgtt/projects/mousepoint-dev/.orchestration/mousepoint-phase2/security-reviewer/status.md`
- Launcher handoff file: `/home/ffgtt/projects/mousepoint-dev/.orchestration/mousepoint-phase2/security-reviewer/handoff.md`

## Objective
## MousePoint 보안 리뷰

이 프로젝트는 C#/.NET 8 WPF 강의용 화면 오버레이 주석 도구입니다. Win32 P/Invoke를 사용합니다.

### 리뷰 포커스

1. **P/Invoke 안전성** (src/MousePoint/Input/NativeMethods.cs)
   - DllImport 선언이 올바른지
   - SetLastError 적절히 사용하는지
   - Marshal 관련 안전성 (구조체 레이아웃, 포인터 변환)

2. **글로벌 훅 보안** (src/MousePoint/Input/GlobalMouseHook.cs)
   - 훅 콜백에서 예외 발생 시 처리
   - CallNextHookEx 항상 호출하는지 (다른 앱 영향)
   - 훅 핸들 유출 방지

3. **윈도우 스타일 조작** (src/MousePoint/MainWindow.xaml.cs)
   - WS_EX_TRANSPARENT 토글의 안전성
   - 윈도우 핸들 유효성 검사

4. **파일 시스템 접근** (src/MousePoint/UI/OnboardingOverlay.cs)
   - 경로 주입 가능성
   - 파일 생성 시 권한 이슈

5. **리소스 누수**
   - IDisposable 패턴 올바른 구현
   - 이벤트 핸들러 해제
   - NotifyIcon 정리

### 출력 형식
보안 이슈를 발견하면 직접 수정하세요. 수정 후 빌드 확인:
```bash
export DOTNET_ROOT=$HOME/.dotnet && export PATH=$PATH:$DOTNET_ROOT
dotnet build MousePoint.sln
```

## Completion
Do not spawn subagents or external agents for this task.
Report results in your final response.
The worker launcher captures your response in `/home/ffgtt/projects/mousepoint-dev/.orchestration/mousepoint-phase2/security-reviewer/handoff.md` automatically.
The worker launcher updates `/home/ffgtt/projects/mousepoint-dev/.orchestration/mousepoint-phase2/security-reviewer/status.md` automatically.

# Worker Task: code-reviewer

- Session: `mousepoint-phase2`
- Repo root: `/home/ffgtt/projects/mousepoint-dev`
- Worktree: `/home/ffgtt/projects/mousepoint-dev-mousepoint-phase2-code-reviewer`
- Branch: `orchestrator-mousepoint-phase2-code-reviewer`
- Launcher status file: `/home/ffgtt/projects/mousepoint-dev/.orchestration/mousepoint-phase2/code-reviewer/status.md`
- Launcher handoff file: `/home/ffgtt/projects/mousepoint-dev/.orchestration/mousepoint-phase2/code-reviewer/handoff.md`

## Objective
## MousePoint 코드 리뷰

이 프로젝트는 C#/.NET 8 WPF 강의용 화면 오버레이 주석 도구입니다.

### 리뷰 범위
src/MousePoint/ 전체 소스코드를 리뷰하세요.

### 리뷰 기준
1. **코드 품질**: 네이밍, 구조, 가독성, C# 컨벤션
2. **아키텍처**: 관심사 분리, 의존성 방향, SOLID 원칙
3. **WPF 패턴**: MVVM 위반 없는지, 리소스 관리, Dispatcher 사용
4. **메모리 관리**: IDisposable 패턴, 이벤트 해제, Canvas 요소 정리
5. **에러 처리**: null 체크, 예외 처리, 방어적 프로그래밍
6. **성능**: CompositionTarget.Rendering 사용 패턴, 불필요한 할당, 링 버퍼 효율성

### 출력 형식
이슈를 발견하면 직접 수정하세요 (Edit 도구 사용). 수정할 수 없는 구조적 이슈는 handoff에 기록하세요.

### 빌드 검증
수정 후 반드시 빌드가 성공하는지 확인:
```bash
export DOTNET_ROOT=$HOME/.dotnet && export PATH=$PATH:$DOTNET_ROOT
dotnet build MousePoint.sln
```

## Completion
Do not spawn subagents or external agents for this task.
Report results in your final response.
The worker launcher captures your response in `/home/ffgtt/projects/mousepoint-dev/.orchestration/mousepoint-phase2/code-reviewer/handoff.md` automatically.
The worker launcher updates `/home/ffgtt/projects/mousepoint-dev/.orchestration/mousepoint-phase2/code-reviewer/status.md` automatically.

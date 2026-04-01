---
name: ask-agent-mode
enabled: false
event: all
tool_matcher: Agent
action: block
conditions:
  - field: description
    operator: not_contains
    pattern: "[confirmed:subagent]"
---

⛔ **Agent 도구 호출이 차단되었습니다.**

병렬 에이전트를 실행하기 전에 반드시 사용자에게 실행 모드를 물어보세요.

**AskUserQuestion으로 다음을 물어보세요:**
- **Sub-agent 모드**: 현재 세션 내에서 백그라운드 서브에이전트로 실행 (pane 없음, 결과만 반환)
- **Tmux 모드**: 현재 tmux 세션에 pane을 split하여 독립 claude 워커 실행 (실시간 확인 가능)

**사용자가 선택한 후:**
- Sub-agent 선택 시: Agent 도구의 description 끝에 `[confirmed:subagent]`를 추가하여 재호출
- Tmux 선택 시: `tmux split-window`로 현재 세션에 pane을 만들고 `claude -p` 실행

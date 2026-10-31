# Developers Profile (Universal)

Role pack for **feature work, bugfixes, and pre-merge code review**.

> **CUSTOMIZE:** Fill `Docs/cursor-agent-kit/PROJECT.md`. Map `SRC_ROOT`, `CODE_LAYERS`, `BUILD_COMMAND`, `TEST_COMMAND_*`.

## Contents

| Folder | Purpose |
|--------|---------|
| `rules/` | Always-on / scoped Cursor rules (`.mdc`) |
| `skills/` | Agent skills (`SKILL.md`) |
| `playbooks/` | Paste-into-chat mission briefings |

## When to use which playbook

| Goal | Playbook |
|------|----------|
| New feature / refactor | `playbooks/request.md` |
| Persistent bug after failed fixes | `playbooks/refresh.md` |
| Pre-merge review | `playbooks/review.md` |
| End-of-session doctrine update | `playbooks/retro.md` |

## Quality gates (from PROJECT.md)

1. Prefer unit tests for pure logic (`TEST_COMMAND_UNIT`).
2. Prefer integration/E2E for multi-component flows (`TEST_COMMAND_INTEGRATION`).
3. Do not put product regressions under `SKIP_OR_DEMO_CATEGORY`.
4. Run `BUILD_COMMAND` when the change can break compile/load.

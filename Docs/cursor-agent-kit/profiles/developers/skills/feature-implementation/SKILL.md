---
name: feature-implementation
description: Phased feature/refactor execution (recon → plan → implement → verify → self-audit). Use for new features, planned refactors, API/config contract changes, or when the user pastes the developers request playbook.
---

# Feature Implementation (Developers)

> **CUSTOMIZE:** Resolve `SRC_ROOT`, `CODE_LAYERS`, `TEST_*`, `BUILD_COMMAND` from `Docs/cursor-agent-kit/PROJECT.md`.

## Inline TODO ledger

Maintain `✅` / `🚧` / `⚠️` across phases.

## Phase 0 — Recon (read-only)

Collect evidence:

- Entry points under `SRC_ROOT` / `CODE_LAYERS`
- Related configs/assets under `DATA_CONFIG_ROOT`
- Existing similar feature pattern
- Tests that already cover adjacent behavior
- Backend contract if `BACKEND_ROOT` is involved

Output: architecture digest ≤200 lines. **No mutations.**

## Phase 1 — Plan

1. Success criteria (user-visible + technical)
2. Full impact surface (files, consumers, assets, tests, CI)
3. Strategy aligned with existing layers
4. Clarification only for true blockers

## Phase 2 — Execute

- Incremental diffs; Read → Write → Re-read
- Update **all** consumers of shared contracts in-session
- Valid sidecars for new assets (stack-specific)
- Keep analysis in chat (no unsolicited report files)

## Phase 3 — Verify

Run the smallest honest gates:

1. Unit if pure logic (`TEST_COMMAND_UNIT`)
2. Integration if multi-component flow (`TEST_COMMAND_INTEGRATION`)
3. Fix failures autonomously before proceeding

Exclude `SKIP_OR_DEMO_CATEGORY` unless the task is that demo pipeline.

## Phase 4 — Zero-trust audit

- Fresh git/diff review
- Re-check one nearby critical workflow from `PROJECT.md` hot paths
- Confirm consumers still consistent

## Phase 5 — Final report

```
### Changes Applied
- modified/created: ...

### Verification Evidence
- ...

### System-Wide Impact Statement
- ...

### Final Verdict
- "Self-Audit Complete. System state is verified and consistent. No regressions identified. Mission accomplished."
```

Or halt with the CRITICAL ISSUE verdict line.

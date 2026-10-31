---
name: pre-merge-code-review
description: Pre-merge code review for developer PRs. Use when user asks for review before merge, PR review, diff review, or pastes the developers review playbook.
---

# Pre-Merge Code Review (Developers)

> **CUSTOMIZE:** Hot paths and CI file from `PROJECT.md`.

## Inputs

- PR URL / branch / or local diff
- Stated intent of the change

## Method

1. `git status`, `git diff`, and commits vs base
2. Read every changed production file; skim generated/noise
3. Trace shared contract consumers for modified public APIs/types/events/config fields
4. Check stack specifics (sidecars, module boundaries, tooling leaking into runtime)
5. Evaluate tests: added/updated? right unit vs integration? accidental demo/skip-only?
6. CI impact on `CI_WORKFLOW_PATH`

## Severity

- **P0** ship-stopper (crash, data loss, broken core flow, secrets)
- **P1** high regression risk / missing mandatory tests for changed contract
- **P2** maintainability / clarity
- **P3** nit

## Output (mandatory)

```
Verdict: APPROVE | REQUEST CHANGES | BLOCK
Summary: 1-3 lines
Findings:
- [Px] path:line — issue — impact — fix
Coverage:
- What is tested / what is missing
CI:
- Expected unit/integration effect
Residual risks:
- ...
```

No rubber-stamp approvals.

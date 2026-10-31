---
name: fix-autotests
description: Diagnose and fix failing or flaky Unity autotests in Echoes of Ascent. Use when tests fail locally/CI, flakes appear, or user pastes the QA fix-tests playbook.
---

# Fix Autotests (QA)

## Hard gates

1. Capture failing signal (test name, log excerpt, CI job)
2. Classify: product bug vs obsolete test vs flake vs infra
3. No assert deletion / category smuggling to go green
4. Prefer deterministic waits and factory fixes
5. If product bug: keep failing test (or add one) and report clearly unless user asks QA to patch product

## Phases

### 0 — Baseline (read-only)

Job: EditMode/PlayMode; category filters; artifact paths; recent diff touching SUT or tests.

### 1 — Reproduce

Re-run single test if possible. Note determinism (always/often/rare).

### 2 — Classify + RCA

Hypothesis loops. Common Unity flake sources: physics timing, missing `yield`, leaked objects, static state, SO mutation, camera/input not ready.

### 3 — Fix

- Flake/infra/obsolete test → fix tests/factory/CI
- Product regression → document + keep signal red (or fix product if requested)

### 4 — Verify

Failing signal now correct (pass for test bugs; still fail for product bugs with clear assertion). Run neighbors in same class.

### 5 — Audit + report

Root class, changes, evidence, CI notes.

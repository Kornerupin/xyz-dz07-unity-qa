---
name: deep-rca-remediation
description: Deep RCA for persistent product bugs. Use when prior fixes failed, user asks for root cause / minimal repro / failing test first, or pastes the developers refresh playbook.
---

# Deep RCA Remediation (Developers)

> **CUSTOMIZE:** Log/artifact locations and test dirs come from `PROJECT.md`.

## Hard gates

- Phase 0 read-only baseline
- No fix until reproducible on command (prefer failing automated test)
- Fix only after hypothesis confirmed by evidence
- Minimal durable fix; cascade to shared consumers
- Verify + zero-trust audit before done

## Ledger

Track phases 0–6 with `✅` / `🚧` / `⚠️`.

## Phase 0 — Baseline (read-only)

Digest: symptom, expected, frequency, environment, suspected systems, log excerpts (local logs, CI artifacts).

## Phase 1 — Isolate

- Define correctness bullets
- Prefer failing automated test under `TEST_ROOT`
- Pinpoint trigger (inputs, data, timing, environment)

## Phase 2 — RCA loop

Hypothesis → safe experiment → evidence → accept/reject. Forbidden: symptom-only patches without mechanism; retrying failed fixes without new data.

## Phase 3 — Remediation

Smallest fix at the invariant. Read → Write → Re-read.

## Phase 4 — Verification

Re-run Phase 1 signal; run relevant unit/integration; exclude `SKIP_OR_DEMO_CATEGORY` unless relevant.

## Phase 5 — Audit

Nearby workflow + common regressions for the stack (lifecycle, caching, concurrency, config mutation).

## Phase 6 — After-action

Root Cause / Remediation / Verification Evidence / required verdict line.

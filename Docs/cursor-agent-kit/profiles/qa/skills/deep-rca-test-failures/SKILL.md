---
name: deep-rca-test-failures
description: Deep RCA for persistent Unity test failures/flakes in Echoes of Ascent after simpler fixes failed. Use for stubborn CI red, heisenbugs, or QA refresh playbook.
---

# Deep RCA — Test Failures (QA)

Same discipline as developer RCA, specialized for test signals.

## Gates

- Phase 0 read-only
- Repro on command before fixes
- Evidence-backed root cause
- Minimal durable remediation
- Verify neighbors + category/CI assumptions

## Extra Unity QA probes

- Shared static / ScriptableObject dirty state across tests
- Physics/FixedUpdate vs yield timing
- Object lifecycle (`Destroy` vs `DestroyImmediate` EditMode)
- Input injection timing vs player snap helpers
- CI-only: license, graphics, path, category filter (`!DemoAI`)
- Artifact logs via known paths used by `analyze_unity_logs.py`

## Final report

Root Cause / Remediation / Verification / Verdict line (mission accomplished or CRITICAL ISSUE).

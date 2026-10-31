---
name: write-autotests
description: Write Unity EditMode/PlayMode autotests for Echoes of Ascent using existing BDD factory and TC patterns. Use when adding test coverage, GDD scenarios, or pasting the QA write-tests playbook.
---

# Write Autotests (QA)

## Ledger

`✅` / `🚧` / `⚠️` across phases.

## Phase 0 — Recon (read-only)

- Existing coverage in EditMode + `GddPositiveBddIntegrationTests`
- Factory capabilities in `BddTestSceneFactory`
- Production seams available for injection
- GDD / requested acceptance criteria
- CI category rules

Digest ≤200 lines. No mutations.

## Phase 1 — Plan

1. Success criteria (which behaviors become guarded)
2. Case list: happy / boundary / negative (if in scope)
3. EditMode vs PlayMode choice per case
4. Factory extensions needed
5. CI impact

## Phase 2 — Implement

- Prefer add to existing test classes when cohesive
- Extend factory for reusable fixtures
- SetUp/TearDown with `Reset()` for PlayMode
- Valid `.meta` for new files
- Do not use `DemoAI` for product tests

## Phase 3 — Verify

Run targeted tests; fix compile/setup issues; confirm isolation.

## Phase 4 — Audit

- Naming clarity
- Assert strength
- No cross-test pollution
- Category filters

## Phase 5 — Report

Changes / evidence / coverage map (TC → test) / verdict.

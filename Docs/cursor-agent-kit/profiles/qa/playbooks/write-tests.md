{What to cover: GDD TCs, bug ticket, feature acceptance criteria. Include EditMode/PlayMode preference if any.}

---

## Mission Briefing: QA — Write Autotests

Execute under the **QA** Echoes of Ascent doctrine.

### Phase 0 — Recon (read-only)

Inventory existing EditMode/PlayMode coverage, `BddTestSceneFactory`, CI categories. Digest ≤200 lines.

### Phase 1 — Plan

Case list + EditMode/PlayMode split + factory extensions + success criteria.

### Phase 2 — Implement

Extend existing patterns/TC style. SetUp/TearDown reset. No `DemoAI` on product tests. Valid `.meta`.

### Phase 3 — Verify

Run targeted tests; fix setup failures; confirm isolation.

### Phase 4 — Audit

Assert quality, naming, CI alignment.

### Phase 5 — Report

Changes / evidence / TC→test map / verdict.

Ledger: ✅ / ⚠️ / 🚧.

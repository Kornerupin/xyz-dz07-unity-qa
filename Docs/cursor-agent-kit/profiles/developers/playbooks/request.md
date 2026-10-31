{Your feature, refactor, or change request here. Be specific about WHAT and WHY.}

---

## Mission Briefing: Developers — Feature / Change SOP

> **CUSTOMIZE:** Agent reads `Docs/cursor-agent-kit/PROJECT.md` for `SRC_ROOT`, tests, CI.

Execute under the **Developers** universal doctrine. Phases are mandatory.

### Phase 0 — Reconnaissance (read-only)

Scan `SRC_ROOT`, related data/assets, and adjacent tests. Digest ≤200 lines. No mutations.

### Phase 1 — Planning

Success criteria, full impact surface (code + assets + tests + CI), strategy justified against existing layers.

### Phase 2 — Execution

Incremental implementation. Read → Write → Re-read. Update all consumers of shared contracts. Valid sidecars for new assets.

### Phase 3 — Verification

Relevant unit and/or integration gates (`TEST_COMMAND_*`). Exclude `SKIP_OR_DEMO_CATEGORY` unless this task is that demo pipeline. Autofix failures.

### Phase 4 — Zero-trust self-audit

Fresh evidence. Hunt one nearby regression path from `PROJECT.md` hot paths.

### Phase 5 — Final report

Changes Applied / Verification Evidence / System-Wide Impact / Final Verdict (mission accomplished **or** CRITICAL ISSUE halt).

Maintain inline TODO ledger with ✅ / ⚠️ / 🚧.

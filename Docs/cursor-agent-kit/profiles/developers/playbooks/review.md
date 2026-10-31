{PR URL, branch name, or “review current working tree”. Include intent if not obvious.}

---

## Mission Briefing: Developers — Pre-Merge Code Review

Perform a skeptical review. Do not implement unless asked to fix findings.

> **CUSTOMIZE:** Walk hot paths listed in `PROJECT.md`.

### Checks

1. Intent vs diff
2. Shared contract blast radius
3. Stack safety (sidecars, module boundaries)
4. Regression paths from `PROJECT.md`
5. Test adequacy (unit vs integration; no hiding coverage under `SKIP_OR_DEMO_CATEGORY`)
6. CI implications (`CI_WORKFLOW_PATH`)
7. Secrets / junk files

### Output

`Verdict: APPROVE | REQUEST CHANGES | BLOCK` plus severity-tagged findings, coverage gaps, residual risks.

Evidence-based only — cite paths/lines.

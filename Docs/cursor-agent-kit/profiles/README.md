# Cursor Agent Profiles (Universal Kit)

Two role packs for Cursor agents. **Generic by default** — fill `PROJECT_CUSTOMIZATION.template.md` (save as `PROJECT.md`) with your repo anchors.

| Profile | Path | Use for |
|---------|------|---------|
| **Developers** | `developers/` | Features, bugfixes, pre-merge review |
| **QA** | `qa/` | Write/fix autotests, test review |

> **CUSTOMIZE:** Everywhere you see a `> **CUSTOMIZE**` callout or `REPLACE_*` token, point values at your filled `PROJECT.md`.

## What is what

| Artifact | Who loads it | How you use it |
|----------|--------------|----------------|
| **Rules** (`.mdc`) | Cursor automatically when copied to `.cursor/rules/` | Always-on or glob-scoped doctrine |
| **Skills** (`SKILL.md`) | Cursor when copied to `.cursor/skills/<name>/` | Auto-picked by task description, or `@skill-name` |
| **Playbooks** (`.md`) | **You** paste into chat | One-shot mission briefing for a session |

Playbooks are optional but recommended for juniors: they force phases without relying on skill discovery.

## Activate a profile

Cursor reads `.cursor/rules/` and `.cursor/skills/` (not this kit folder).

```powershell
# From repo root — QA example
New-Item -ItemType Directory -Force -Path ".cursor\rules", ".cursor\skills" | Out-Null
Copy-Item -Recurse -Force "Docs\cursor-agent-kit\profiles\qa\rules\*" ".cursor\rules\"
Copy-Item -Recurse -Force "Docs\cursor-agent-kit\profiles\qa\skills\*" ".cursor\skills\"
```

```powershell
# Developers
Copy-Item -Recurse -Force "Docs\cursor-agent-kit\profiles\developers\rules\*" ".cursor\rules\"
Copy-Item -Recurse -Force "Docs\cursor-agent-kit\profiles\developers\skills\*" ".cursor\skills\"
```

Clear the target folders first when switching roles to avoid mixed doctrine.

**Option B:** paste a playbook from `*/playbooks/*.md` into chat (replace the `{…}` first line with your task).

## First-time setup checklist

1. Copy this kit into the target repo (or clone a repo that already vendors it under `Docs/cursor-agent-kit/`).
2. Fill `PROJECT_CUSTOMIZATION.template.md` → save as `PROJECT.md`.
3. Search profiles for `CUSTOMIZE` / `REPLACE_` and align globs/paths if your layout differs strongly.
4. Activate Developers or QA (copy commands above).
5. Smoke-test: paste a playbook with a tiny task; confirm the agent follows phases.

## Stack assumption

Default wording targets **automated tests + CI** (Unity Test Framework examples in callouts). The process works for Jest/pytest/xUnit/etc. if you map folders and commands in `PROJECT.md`.

# QA Profile — Echoes of Ascent

Role pack for **writing/fixing Unity autotests** and **reviewing test/code quality**.

## Contents

| Folder | Purpose |
|--------|---------|
| `rules/` | Cursor rules (`.mdc`) |
| `skills/` | Agent skills |
| `playbooks/` | Paste-into-chat mission briefings |

## When to use which playbook

| Goal | Playbook |
|------|----------|
| New autotests / BDD scenarios | `playbooks/write-tests.md` |
| Broken/flaky/failing tests | `playbooks/fix-tests.md` |
| Deep persistent test failure | `playbooks/refresh.md` |
| Review test PR / test diff | `playbooks/review.md` |
| End-of-session doctrine update | `playbooks/retro.md` |

## Test map (this repo)

| Kind | Path | Assembly |
|------|------|----------|
| EditMode | `Assets/_Project/Tests/EditMode/` | `EchoesOfAscent.EditMode.Tests` |
| PlayMode | `Assets/_Project/Tests/PlayMode/` | `EchoesOfAscent.PlayMode.Tests` |
| Scene factory | `BddTestSceneFactory.cs` | shared PlayMode helpers |
| GDD BDD | `GddPositiveBddIntegrationTests.cs` | TC-001+ positive scenarios |
| Demo AI only | `CraftTest.cs` `[Category("DemoAI")]` | excluded from normal CI PlayMode |

CI: `.github/workflows/unity_qa.yml`  
Log AI helper (demo pipeline): `analyze_unity_logs.py`

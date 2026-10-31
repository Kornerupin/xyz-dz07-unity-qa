# Project Customization Sheet

Copy this file to your repo as `Docs/cursor-agent-kit/PROJECT.md` (or keep next to the profiles) and fill every `REPLACE` field before activating a profile.

Agents and humans should treat this sheet as the **single source of project anchors**. Skills/rules/playbooks stay generic and point here.

---

## Identity

| Field | Value | Notes |
|-------|-------|-------|
| `PROJECT_NAME` | REPLACE | Short product/repo name |
| `PRIMARY_LANGUAGE` | REPLACE | e.g. C#, TypeScript, Python |
| `PRIMARY_STACK` | REPLACE | e.g. Unity 6 + URP, React + Node, .NET MAUI |

## Source layout

| Field | Value | Notes |
|-------|-------|-------|
| `SRC_ROOT` | REPLACE | Main production code root |
| `CODE_LAYERS` | REPLACE | Folder/layer names if any |
| `EDITOR_OR_TOOLING_ROOT` | REPLACE / N/A | Editor-only or tooling code |
| `DATA_CONFIG_ROOT` | REPLACE / N/A | Configs, ScriptableObjects, YAML, etc. |

## Tests

| Field | Value | Notes |
|-------|-------|-------|
| `TEST_ROOT` | REPLACE | Root of automated tests |
| `UNIT_TEST_DIR` | REPLACE | Fast/unit (EditMode, Jest unit, xUnit, …) |
| `INTEGRATION_TEST_DIR` | REPLACE | Runtime/integration/E2E |
| `TEST_HELPER_FACTORY` | REPLACE / N/A | Shared fixture/factory class or module |
| `TEST_NAMING_PATTERN` | REPLACE | e.g. `TC00X_Condition_Expected`, `describe/it` |
| `TEST_FRAMEWORK` | REPLACE | NUnit / Unity Test Framework / Jest / pytest / … |
| `SKIP_OR_DEMO_CATEGORY` | REPLACE / N/A | Category that must NOT hide product coverage |

## CI / quality gates

| Field | Value | Notes |
|-------|-------|-------|
| `CI_WORKFLOW_PATH` | REPLACE | e.g. `.github/workflows/…` |
| `CI_UNIT_JOB` | REPLACE | Job/step name for unit tests |
| `CI_INTEGRATION_JOB` | REPLACE | Job/step for integration/E2E |
| `CI_CATEGORY_FILTER` | REPLACE / N/A | CLI filter excluding demo-only suites |
| `LOG_ANALYZER_SCRIPT` | REPLACE / N/A | Optional AI/log helper |
| `BUILD_COMMAND` | REPLACE | Local build/verify command |
| `TEST_COMMAND_UNIT` | REPLACE | Local unit test command |
| `TEST_COMMAND_INTEGRATION` | REPLACE | Local integration/E2E command |

## Docs & contracts

| Field | Value | Notes |
|-------|-------|-------|
| `PRODUCT_SPEC_PATH` | REPLACE / N/A | GDD, PRD, OpenAPI, … |
| `BACKEND_ROOT` | REPLACE / N/A | Optional API/service folder |

## Review hot paths (regression checklist)

List 3–8 critical user/system flows reviewers must mentally walk:

1. REPLACE
2. REPLACE
3. REPLACE

## Example fill (Echoes of Ascent — reference only)

| Field | Example |
|-------|---------|
| `PROJECT_NAME` | Echoes of Ascent |
| `SRC_ROOT` | `Assets/_Project/Scripts/` |
| `CODE_LAYERS` | Core, Gameplay, Input, Meta, Presentation |
| `TEST_ROOT` | `Assets/_Project/Tests/` |
| `UNIT_TEST_DIR` | `Assets/_Project/Tests/EditMode/` |
| `INTEGRATION_TEST_DIR` | `Assets/_Project/Tests/PlayMode/` |
| `TEST_HELPER_FACTORY` | `BddTestSceneFactory` |
| `SKIP_OR_DEMO_CATEGORY` | `DemoAI` |
| `CI_WORKFLOW_PATH` | `.github/workflows/unity_qa.yml` |
| `CI_CATEGORY_FILTER` | `-testCategory "!DemoAI"` |
| `PRODUCT_SPEC_PATH` | `Docs/EchoesOfAscent_GDD.pdf` |

Delete this example section in your project copy if it confuses newcomers.

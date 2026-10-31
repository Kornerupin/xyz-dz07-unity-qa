# Project anchors — Echoes of Ascent / ДЗ №7

Справочник текущего учебного проекта. Профили агента необязательны: они помогают ориентироваться в коде, но не заменяют Unity Test Runner, GitHub Actions и API. Для ДЗ не требуется устанавливать набор в `.cursor`.

Главная инструкция: [DZ07_START_HERE.md](../../DZ07_START_HERE.md).

## Проект

| Поле | Значение |
|---|---|
| `PROJECT_NAME` | Echoes of Ascent — учебный проект ДЗ №7 |
| `PROJECT_ROOT` | `S:\Projects\Codex\XYZ` |
| `PRIMARY_LANGUAGE` | C#; Python и PowerShell для инструментов CI |
| `PRIMARY_STACK` | Unity 6000.4.0f1, URP, Input System, Unity Test Framework / NUnit |
| `SRC_ROOT` | `Assets/_Project/Scripts/` |
| `CODE_LAYERS` | Core, Gameplay, Input, Meta, Presentation |
| `EDITOR_OR_TOOLING_ROOT` | `Assets/_Project/Editor/`; инструменты ДЗ в `tools/` |
| `DATA_CONFIG_ROOT` | `Assets/_Project/Data/`, Prefabs, Materials, Shaders |

## Тесты и контролируемая ошибка

| Поле | Значение |
|---|---|
| `TEST_ROOT` | `Assets/_Project/Tests/` |
| `UNIT_TEST_DIR` | `Assets/_Project/Tests/EditMode/` |
| `INTEGRATION_TEST_DIR` | `Assets/_Project/Tests/PlayMode/` |
| `TEST_HELPER_FACTORY` | `BddTestSceneFactory` |
| `TEST_FRAMEWORK` | Unity Test Framework + NUnit |
| `CRAFTING_RULES` | `Assets/_Project/Scripts/Gameplay/CraftingRules.cs` |
| `CRAFTING_TEST` | `Assets/_Project/Tests/PlayMode/CraftTest.cs` |
| `MUTATION_POINT` | `expectedRemainingWood = 3; // COURSE_MUTATION_POINT` |
| `BROKEN_STATE` | Ожидание дерева 4 вместо правильных 3; стоимость крафта не меняется |
| `MUTATION_COMMANDS` | `.\tools\set-craft-state.ps1 broken` / `.\tools\set-craft-state.ps1 green` |
| `LOCAL_HELPER_TESTS` | `tests/test_pipeline.py`: синтетические XML и mock-ответы; не доказательство CI/API |

Категория `DemoAI` сохранена как метка и **не исключается** из автоматического прогона. Исходный режим курса с ручным `demo_ai_pipeline` и фильтром `!DemoAI` заменён запуском по push.

Ожидаемые полные имена крафт-проверок в NUnit XML:

```text
EchoesOfAscent.Tests.PlayMode.CraftTest.Test_CraftSword_ConsumesResources
EchoesOfAscent.Tests.PlayMode.CraftTest.Test_CraftSword_WithoutResources_DoesNotConsumeInventory
```

## CI и анализ

| Поле | Значение |
|---|---|
| `CI_WORKFLOW_PATH` | `.github/workflows/ai_qa_gate.yml` в корне Git-репозитория |
| `CI_TRIGGER` | Каждый push; также workflow_dispatch |
| `CI_TEST_MODES` | All: EditMode и PlayMode; исходные игровые тесты сохраняются |
| `CI_CATEGORY_FILTER` | Исключения DemoAI нет |
| `PASS_GATE` | Успешный Unity-прогон и обе крафт-проверки с `Passed` в XML |
| `LOG_ANALYZER_SCRIPT` | `analyze_unity_logs.py` |
| `RESULT_VERIFIER` | `tools/verify_unity_results.py` |
| `LOCAL_CHECK_COMMAND` | `.\tools\check-local.ps1` |
| `EXPORT_COMMAND` | `.\tools\export-dz07.ps1` — ZIP сохранённой Git-версии |
| `AI_MODEL_DEFAULT` | `gpt-4o-mini` из курса; переопределение через Actions variable `AI_MODEL` |
| `AI_REQUIRED_SECRET` | `AI_API_KEY` |
| `UNITY_PERSONAL_SECRETS` | `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD` |

Анализ запускается через `failure()` после падения Unity. Для отчёта требуется XML с настоящим failed test-case. Issue создаётся только после успешного реального ответа API, проверки структуры и цитат, признака `eligible` и соответствия commit/run. Ошибка активации или отсутствие лога не должны превращаться в выдуманный баг игры. Красный прогон остаётся красным после создания Issue.

Факт прохождения Unity, обращения к API и создания Issue подтверждают внешние логи и ссылки. Локальные проверки и этот справочник не являются подтверждением таких прогонов.

## Документы и области проверки

| Поле | Значение |
|---|---|
| `PRODUCT_SPEC_PATH` | `Docs/EchoesOfAscent_GDD.pdf` |
| `USER_RUNBOOK` | `DZ07_START_HERE.md` |
| `BACKEND_ROOT` | `Backend/` — Express fall-echo API, необязательный для демонстрации крафта |

При изменениях игры проверять затронутые области: swipe → прыжок и углы запуска; посадку → checkpoint; падение → respawn; финиш → интерфейс победы; предпросмотр траектории; сохранение и Echo API. Для ДЗ №7 отдельно контролировать расход ресурсов и отказ крафта без изменения инвентаря.

Остальные ДЗ и Countomania расположены в той же рабочей области, но не входят в публикуемый учебный репозиторий. Не включать в коммиты лицензии, ключи, пароли, сгенерированные логи и результаты.

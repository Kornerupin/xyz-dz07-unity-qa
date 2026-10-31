# ДЗ №7 — запуск тестов и ИИ-анализ без ключа OpenAI

Проект: S:\Projects\Codex\XYZ. Репозиторий: https://github.com/Kornerupin/xyz-dz07-unity-qa . В нём используется учебный Echoes of Ascent из архива курса, Unity 6000.4.0f1 и тест механики крафта.

Первый коммит подготовлен локально, ветка main, origin подключён. Для сдачи ещё нужны реальные зелёный и красный прогоны Actions и публичный автоматический Issue. Подготовленные файлы и локальные проверки не заменяют эти результаты.

## 1. Отправь подготовленный коммит

Открой PowerShell и выполни:

~~~powershell
Set-Location -LiteralPath 'S:\Projects\Codex\XYZ'
git log -1 --oneline
git remote -v
git push -u origin main
~~~

origin настроен на git@github.com:Kornerupin/xyz-dz07-unity-qa.git. Git использует существующее SSH-подключение; вход через браузер и Git Credential Manager не требуются. Для отдельной проверки в своём PowerShell выполни ssh -T git@github.com. Успешный ответ должен содержать Hi Kornerupin! и сообщение об успешной аутентификации; GitHub при этом не предоставляет shell и может вернуть код 1.

Повторные git init, git remote add и начальный git commit не нужны. Адрес HTTPS в ссылке выше предназначен для просмотра репозитория в браузере; Git push использует SSH.

## 2. Проверь подготовленные Unity Secrets

Ты сообщил, что Unity Personal уже настроена. В Settings → Secrets and variables → Actions должны быть три имени: UNITY_LICENSE, UNITY_EMAIL, UNITY_PASSWORD. UNITY_LICENSE содержит полный текст файла Unity_lic.ulf, а не путь. Значения остаются в Secrets и не входят в Git.

Устанавливать Unity Editor на свой компьютер для этого workflow не нужно: редактор запускается на GitHub runner. Локальная установка нужна только при желании запускать Unity-тесты у себя. Версия проекта — 6000.4.0f1.

## 3. Как работает анализ без платного API

Никакой OpenAI-ключ и подписка для анализа не нужны. После настоящего падения теста GitHub запускает отдельную машину, скачивает официальный Ollama 0.40.13 с проверкой SHA-256 и открытую модель qwen2.5:3b-instruct (около 1,9 ГБ). Модель работает на CPU этой машины через локальный адрес 127.0.0.1; облачные функции Ollama отключены. На твой компьютер ничего устанавливать не требуется.

Это реальный автоматический вызов нейросети. Доступ к внешнему платному API не используется. Для публичного репозитория стандартные hosted runners бесплатны по документации GitHub. Скачивание и генерация займут дополнительное время; фактическая длительность будет видна в Actions.

Покупать API-доступ, добавлять AI_API_KEY, создавать личный GitHub token или включать платный сервис не нужно. GITHUB_TOKEN для публикации Issue GitHub выдаёт workflow автоматически.

## 4. Дождись первого зелёного запуска

Открой https://github.com/Kornerupin/xyz-dz07-unity-qa/actions . После push появится AI QA gate - CraftTest.

1. Дождись окончания job craft-gate. Первый импорт Unity может занять значительное время.
2. Итог должен быть зелёным; ai-report должен быть пропущен.
3. В артефакте qa-unity-НОМЕР-ПОПЫТКА найди XML с двумя Passed тестами:
   - EchoesOfAscent.Tests.PlayMode.CraftTest.Test_CraftSword_ConsumesResources
   - EchoesOfAscent.Tests.PlayMode.CraftTest.Test_CraftSword_WithoutResources_DoesNotConsumeInventory
4. Сохрани ссылку на запуск и SHA коммита.

Если упала активация, компиляция или другой исходный тест, сначала исправь эту причину и добейся зелёного запуска. Ошибка лицензии не является требуемой демонстрацией падения крафт-теста.

## 5. Спровоцируй ошибку в ожидании теста

Только после зелёного прогона выполни:

~~~powershell
.\tools\set-craft-state.ps1 broken
git diff -- Assets/_Project/Tests/PlayMode/CraftTest.cs
git add -- Assets/_Project/Tests/PlayMode/CraftTest.cs
git commit -m "Demonstrate incorrect crafting test expectation"
git push
~~~

В diff должна измениться только константа expectedRemainingWood с 3 на 4. Игровое правило сохраняется: 5 дерева минус стоимость 2 даёт остаток 3. Поэтому ошибочно именно ожидание теста.

## 6. Проверь красный запуск и автоматический Issue

1. В craft-gate падает Test_CraftSword_ConsumesResources: Expected 4, But was 3.
2. Шаг Confirm an actual failed Unity test читает реальный XML и подтверждает падение.
3. Job ai-report скачивает эти логи на отдельную машину. Шаг Propagate the confirmed Unity test failure переносит статус уже упавшего теста и ожидаемо отмечается красным. Он не выполняет новый тест и не подделывает лог: проверяет существующий XML. Это нужно, чтобы дальнейшие шаги использовали именно failure() внутри этой job.
4. Install local CPU model runtime и Analyze verified test failure with local AI должны пройти успешно.
5. Create AI bug report должен пройти успешно. Ссылка появится в сводке и на https://github.com/Kornerupin/xyz-dz07-unity-qa/issues .
6. Весь workflow остаётся красным из-за ошибки теста. Успешный анализ внутри красного workflow — ожидаемый результат.

В Issue проверь тип ошибки, гипотезу Root Cause, буквальные цитаты из лога/кода и рекомендации. Диагноз должен различать неверное ожидание теста и ошибку игрового правила. При обрезанном ответе, неверных цитатах или недоступной модели Issue не создаётся.

Скачай qa-unity-* и qa-evidence-* до истечения 30 дней. В них сохраняются XML, логи, точный запрос request.json/prompt.txt, полный ответ api_response.json, проверенный report.json, Markdown-отчёт, status.json, ссылка issue.json и сведения о реальной модели artifacts/ollama/runtime.json.

## 7. Верни корректное ожидание

После завершения красного запуска и проверки Issue:

~~~powershell
.\tools\set-craft-state.ps1 green
git diff -- Assets/_Project/Tests/PlayMode/CraftTest.cs
git add -- Assets/_Project/Tests/PlayMode/CraftTest.cs
git commit -m "Restore correct crafting expectation"
git push
~~~

Дождись ещё одного зелёного прогона. Не отправляй три состояния подряд без ожидания: требуются отдельные завершённые результаты.

## 8. Отправь результат проверяющему

Главный результат — публичная ссылка на автоматически созданный Issue. Приложи также ссылки на первый зелёный и красный Actions, при возможности — на зелёный после восстановления. Проверь доступность ссылок без входа в аккаунт.

Текст для LMS после реального выполнения:

~~~text
Настроил CI/CD для учебного Unity-проекта: EditMode и PlayMode-тесты, автоматический анализ падения и создание GitHub Issue по условию failure(). Для анализа использовал локальную модель Qwen через Ollama на машине GitHub Actions.

После успешного прогона изменил ожидаемый остаток дерева в тесте крафта с 3 на 4. Падение запустило нейросеть; созданный Issue содержит тип ошибки, Root Cause и рекомендации. Затем восстановил ожидание и получил зелёный прогон.

Автоматический баг-репорт: [ссылка]
Успешный запуск до поломки: [ссылка]
Запуск с ошибкой и ИИ-анализом: [ссылка]
Успешный запуск после восстановления: [ссылка]
Репозиторий: https://github.com/Kornerupin/xyz-dz07-unity-qa
~~~

Для ZIP текущего коммита можно выполнить .\tools\export-dz07.ps1 . ZIP дополняет ссылки, но не заменяет их.

## Если что-то остановилось

- Ошибка SSH: выполни ssh -T git@github.com в обычном PowerShell. При Permission denied (publickey) нужно проверить, что существующий ключ добавлен в аккаунт Kornerupin и доступен используемому SSH-клиенту. Не заменяй ключи и не переходи на HTTPS для обхода этой проверки.
- Нет Actions: проверь .github/workflows/ai_qa_gate.yml в ветке main и разрешение Actions в Settings.
- Ошибка Unity activation: перепроверь три Unity Secrets и официальную инструкцию GameCI.
- Упала установка/загрузка модели: открой соответствующий шаг и artifacts/ollama/pull.log; затем Re-run failed jobs. Артефакт исходного Unity запуска передаётся по имени из родительской job.
- Анализатор отклонил ответ: проверь artifacts/ai/status.json и api_response.json. Не заменяй диагноз вручную; повтори реальную генерацию после исправления причины. Если не хватает времени/памяти, эти ограничения видны в логе.
- Анализ успешен, Issue нет: проверь Settings → General → Issues и разрешение issues: write. Оно уже указано в YAML. Широкий личный токен не нужен.

## Локальная проверка

.\tools\check-local.ps1 запускает 38 Python- и 7 JavaScript-проверок с обозначенными тестовыми ответами. Это проверки кода анализатора и защиты Issue; они не заменяют Unity, реальную генерацию и публичные Actions.

## Источники

- https://docs.ollama.com/capabilities/structured-outputs
- https://docs.ollama.com/api/authentication
- https://ollama.com/library/qwen2.5:3b-instruct
- https://docs.github.com/en/actions/reference/runners/github-hosted-runners
- https://game.ci/docs/github/activation/

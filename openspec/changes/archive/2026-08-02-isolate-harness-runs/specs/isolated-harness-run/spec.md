## ADDED Requirements

### Requirement: Уникальная директория запуска
Harness runner SHALL до запуска model subprocesses создать отдельный directory `.harness-runs/<skill-id>/<run-id>`, где `<skill-id>` является ID проверяемого domain skill, а `<run-id>` состоит из UTC timestamp с точностью до секунды и короткого случайного suffix. Создание SHALL исключать повторное использование существующего directory.

#### Scenario: Два запуска начинаются одновременно
- **WHEN** два процесса запускают harness для одного domain skill в одну секунду
- **THEN** каждый процесс использует собственный run directory и не перезаписывает файлы другого процесса

### Requirement: Изолированные scenario artifacts
Harness runner SHALL для каждого обнаруженного scenario создать `<run-directory>/scenarios/<scenario-id>`, сгенерировать туда `metrics.json` из raw fixtures, создать `result.json` через проверяемый domain skill и записать `evaluation.json` через существующий evaluator. Generated `metrics.json`, `result.json` и `evaluation.json` SHALL создаваться только внутри run directory.

#### Scenario: Успешный массовый запуск
- **WHEN** harness обрабатывает все `scenario-*` из `eval/scenarios`
- **THEN** run directory содержит отдельный scenario directory с generated `metrics.json`, `result.json` и `evaluation.json` для каждого обработанного scenario

#### Scenario: Запуск прерывается после части scenarios
- **WHEN** process завершается до обработки всех scenarios
- **THEN** уже созданный run directory и записанные в него inputs и outputs сохраняются для диагностики

### Requirement: Метаданные происхождения запуска
Harness runner SHALL создать `<run-directory>/run.json` до запуска model subprocesses. Документ SHALL содержать `runId`, `skillId`, repository-relative `skillPath`, UTC `startedAt`, точный requested `model`, `repositoryCommit`, `repositoryDirty` и `repositoryState`.

#### Scenario: Запуск из clean repository
- **WHEN** repository имеет `HEAD` и рабочее дерево clean
- **THEN** `repositoryCommit` содержит фактический commit hash, `repositoryDirty` равен `false`, а `repositoryState` равен `clean`

#### Scenario: Запуск из dirty repository
- **WHEN** repository имеет `HEAD` и рабочее дерево содержит изменения
- **THEN** `repositoryCommit` содержит фактический commit hash, `repositoryDirty` равен `true`, а `repositoryState` равен `dirty`

#### Scenario: Commit отсутствует или Git недоступен
- **WHEN** repository не имеет commit либо Git metadata нельзя прочитать
- **THEN** `repositoryCommit` равен JSON `null`, а `repositoryState` явно сообщает `unborn` или `unavailable`

### Requirement: Неизменность scenario source tree
Harness runner SHALL использовать `eval/scenarios` только как read-only source inputs и SHALL NOT создавать, изменять или удалять файлы внутри него. Repository SHALL игнорировать `/.harness-runs/` через `.gitignore`.

#### Scenario: Harness завершается с success или failure
- **WHEN** верхнеуровневый запуск завершился с любым exit code
- **THEN** содержимое `eval/scenarios` совпадает с состоянием до запуска, а созданные run artifacts не появляются в `git status`

### Requirement: Domain skill identity
Harness runner SHALL записывать и использовать ID проверяемого domain skill, а не ID orchestration skill. Model subprocess SHALL получать точный run-local `metrics.json` и SHALL NOT получать ground truth, committed result или evaluation как analysis input.

#### Scenario: CLI запускает анализ тренировок
- **WHEN** `RunHarness.cs` запускается для `analyze-training-progress`
- **THEN** run path и `run.json.skillId` используют `analyze-training-progress`, а `run.json.skillPath` указывает на `.opencode/skills/analyze-training-progress`

### Requirement: Failed runs сохраняются
Harness runner MUST NOT автоматически удалять run directory после scenario failure, contract failure или orchestration failure.

#### Scenario: Один model subprocess не создаёт валидный result
- **WHEN** evaluator или contract checker отвергает output scenario
- **THEN** run directory, copied metrics и все доступные generated outputs остаются на диске

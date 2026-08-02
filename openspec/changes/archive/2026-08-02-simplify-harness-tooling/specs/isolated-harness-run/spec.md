## MODIFIED Requirements

### Requirement: Изолированные scenario artifacts
Harness runner SHALL для каждого обнаруженного source scenario создать `<run-directory>/scenarios/<scenario-id>`, сгенерировать туда `metrics.json` из raw fixtures, создать `result.json` через проверяемый domain skill и записать `evaluation.json` через evaluator. Source scenario SHALL содержать raw fixtures, `ground-truth.json` и README, но SHALL NOT содержать `metrics.json`, `result.json` или `evaluation.json`.

#### Scenario: Успешный массовый запуск
- **WHEN** harness обрабатывает все `scenario-*` из `eval/scenarios`
- **THEN** run directory содержит отдельный scenario directory с generated `metrics.json`, `result.json` и `evaluation.json` для каждого обработанного scenario

#### Scenario: Запуск прерывается после части scenarios
- **WHEN** process завершается до обработки всех scenarios
- **THEN** уже созданный run directory и записанные в него inputs и outputs сохраняются для диагностики

### Requirement: Неизменность scenario source tree
Harness runner SHALL использовать `eval/scenarios` только как read-only source raw fixtures, acceptance rules и документацию и SHALL NOT создавать, изменять или удалять файлы внутри него. Repository SHALL игнорировать `/.harness-runs/` через `.gitignore`.

#### Scenario: Harness завершается с success или failure
- **WHEN** верхнеуровневый запуск завершился с любым exit code
- **THEN** содержимое `eval/scenarios` совпадает с состоянием до запуска, а созданные run artifacts не появляются в `git status`

### Requirement: Domain skill identity
Harness runner SHALL принимать ID и path проверяемого domain skill и точный model ID. Model subprocess SHALL получать точный run-local `metrics.json` и SHALL NOT получать ground truth, evaluation или любой ранее созданный analysis result как input.

#### Scenario: CLI запускает анализ тренировок
- **WHEN** `RunHarness.cs` запускается для `analyze-training-progress`
- **THEN** run path и `run.json.skillId` используют `analyze-training-progress`, а `run.json.skillPath` указывает на `.opencode/skills/analyze-training-progress`

### Requirement: Failed runs сохраняются
Harness runner MUST NOT автоматически удалять run directory после scenario generation, evaluation или orchestration failure.

#### Scenario: Один model subprocess не создаёт валидный result
- **WHEN** model subprocess или evaluator отвергает output scenario
- **THEN** run directory, run-local metrics и все доступные generated outputs остаются на диске

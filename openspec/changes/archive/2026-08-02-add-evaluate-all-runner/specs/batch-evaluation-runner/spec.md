## ADDED Requirements

### Requirement: Массовый запуск evaluator
Repository SHALL предоставлять `eval/scripts/EvaluateAll.cs`, который при запуске из repository root находит непосредственные каталоги `scenario-*` в `eval/scenarios`, сортирует их ordinal-лексикографически и последовательно запускает `Evaluate.cs` для каждого каталога.

#### Scenario: Набор содержит несколько scenarios
- **WHEN** `eval/scenarios` содержит несколько каталогов `scenario-*`
- **THEN** `EvaluateAll.cs` запускает `Evaluate.cs -- --scenario <scenario-directory>` ровно один раз для каждого каталога в сортированном порядке

### Requirement: Запись стандартных evaluation outputs
Mass runner SHALL не передавать `--output` дочернему evaluator, поэтому каждый запуск `Evaluate.cs` создаёт или перезаписывает `evaluation.json` внутри своего scenario.

#### Scenario: Evaluation output уже существует
- **WHEN** в scenario существует `evaluation.json`
- **THEN** запуск через `EvaluateAll.cs` передаёт управление стандартному output behavior `Evaluate.cs` для этого scenario

### Requirement: Отсутствие post-evaluation проверки
Mass runner SHALL дожидаться завершения каждого дочернего evaluator и SHALL продолжать запуск оставшихся scenarios независимо от exit code завершившегося evaluator. Он SHALL NOT запускать normalizer, читать или сравнивать `evaluation.json`, агрегировать evaluation statuses либо интерпретировать checks.

#### Scenario: Один evaluator возвращает failed или manual review
- **WHEN** запуск `Evaluate.cs` для одного scenario завершается с exit code `2` или `3`
- **THEN** `EvaluateAll.cs` продолжает запуск evaluator для всех последующих scenarios без чтения его output

### Requirement: Ошибки orchestration
Mass runner SHALL завершаться с non-zero, если каталог `eval/scenarios` отсутствует, не содержит каталогов `scenario-*` или невозможно создать дочерний process evaluator. Дочерние exit codes SHALL NOT самостоятельно менять exit code mass runner.

#### Scenario: Scenarios не найдены
- **WHEN** `eval/scenarios` не содержит каталогов `scenario-*`
- **THEN** `EvaluateAll.cs` сообщает ошибку и завершается с non-zero

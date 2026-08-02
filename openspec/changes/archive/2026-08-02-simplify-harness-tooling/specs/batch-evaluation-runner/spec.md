## MODIFIED Requirements

### Requirement: Массовый запуск evaluator
Repository SHALL предоставлять один верхнеуровневый file-based C# runner, который при запуске из repository root находит непосредственные каталоги `scenario-*` в `eval/scenarios`, сортирует их ordinal-лексикографически, создаёт изолированный harness run и запускает normalizer, model generation и `Evaluate.cs` ровно один раз для каждого scenario. Runner SHALL использовать установленные команды `opencode` и `eval/scripts/Evaluate.cs` без CLI overrides и SHALL NOT копировать committed analysis results.

#### Scenario: Набор содержит несколько scenarios
- **WHEN** `eval/scenarios` содержит несколько каталогов `scenario-*`
- **THEN** runner создаёт отдельный run-local scenario directory и выполняет normalization, generation и evaluation ровно один раз для каждого каталога в сортированном порядке

### Requirement: Запись стандартных evaluation outputs
Mass runner SHALL передавать `Evaluate.cs` explicit run-local `--result` и `--output`, поэтому каждый evaluator создаёт `<run-directory>/scenarios/<scenario-id>/evaluation.json`. Generated `metrics.json`, `result.json` и `evaluation.json` MUST NOT записываться в source scenario.

#### Scenario: Source scenario не содержит generated outputs
- **WHEN** mass runner обрабатывает source scenario с raw fixtures и `ground-truth.json`
- **THEN** все generated outputs создаются только в соответствующем run-local scenario directory

### Requirement: Post-evaluation проверка
Mass runner SHALL дожидаться завершения каждого model subprocess и evaluator, SHALL продолжать обработку независимых scenarios после failure и SHALL читать generated evaluation summary для корректной интерпретации `manual_review`. Итоговый status SHALL агрегировать generation и evaluation statuses всех scenarios без отдельного post-run contract stage.

#### Scenario: Один evaluator возвращает failed
- **WHEN** запуск model или `Evaluate.cs` для одного scenario завершается с failure
- **THEN** `RunHarness.cs` продолжает остальные scenarios, сохраняет run и возвращает non-zero

#### Scenario: Evaluation требует только semantic review
- **WHEN** evaluator возвращает code `3`, а generated `evaluation.json.summary.failed` равен `0`
- **THEN** mass runner считает automated scenario checks прошедшими

### Requirement: Ошибки orchestration
Mass runner SHALL завершаться с non-zero, если каталог `eval/scenarios` отсутствует, не содержит каталогов `scenario-*`, невозможно создать run или дочерний process, отсутствует обязательный run artifact либо любой scenario имеет failed automated checks. Он SHALL завершаться с zero только когда generation и evaluation всех scenarios прошли automated validation.

#### Scenario: Scenarios не найдены
- **WHEN** `eval/scenarios` не содержит каталогов `scenario-*`
- **THEN** `RunHarness.cs` сообщает ошибку и завершается с non-zero

#### Scenario: Один scenario не создал result
- **WHEN** model subprocess завершается без run-local `result.json`
- **THEN** mass runner отмечает scenario как failed, продолжает обработку остальных scenarios и завершает верхнеуровневую команду с non-zero

#### Scenario: Все automated checks проходят
- **WHEN** generation завершена и каждый scenario имеет zero failed evaluation checks
- **THEN** верхнеуровневая команда завершается с zero

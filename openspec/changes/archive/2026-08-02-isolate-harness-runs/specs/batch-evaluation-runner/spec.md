## MODIFIED Requirements

### Requirement: Массовый запуск evaluator
Repository SHALL предоставлять верхнеуровневый file-based C# runner, который при запуске из repository root находит непосредственные каталоги `scenario-*` в `eval/scenarios`, сортирует их ordinal-лексикографически, создаёт изолированный harness run и запускает model generation и `Evaluate.cs` ровно один раз для каждого scenario. Runner SHALL NOT удалять scenario snapshots.

#### Scenario: Набор содержит несколько scenarios
- **WHEN** `eval/scenarios` содержит несколько каталогов `scenario-*`
- **THEN** runner создаёт отдельный run-local scenario directory и выполняет generation и evaluation ровно один раз для каждого каталога в сортированном порядке

### Requirement: Запись стандартных evaluation outputs
Mass runner SHALL передавать `Evaluate.cs` explicit run-local `--result` и `--output`, поэтому каждый evaluator создаёт `<run-directory>/scenarios/<scenario-id>/evaluation.json` и не использует стандартный output path committed scenario.

#### Scenario: Committed evaluation output уже существует
- **WHEN** в исходном scenario существует `evaluation.json`
- **THEN** mass runner сохраняет исходный файл без изменений и записывает новый output только в run-local scenario directory

### Requirement: Post-evaluation и contract проверка
Mass runner SHALL дожидаться завершения каждого model subprocess и evaluator, SHALL продолжать обработку независимых scenarios после failure и SHALL запускать существующие contract checks для run outputs. Он SHALL читать generated evaluation summary для корректной интерпретации `manual_review` и агрегировать все scenario и contract statuses.

#### Scenario: Evaluation требует только semantic review
- **WHEN** evaluator возвращает code `3`, а generated `evaluation.json.summary.failed` равен `0`
- **THEN** mass runner считает automated scenario checks прошедшими

#### Scenario: Один evaluator возвращает failed
- **WHEN** evaluator возвращает code `2` либо generated summary содержит failed checks
- **THEN** mass runner продолжает остальные scenarios, сохраняет failed output и учитывает scenario как failure итогового запуска

#### Scenario: Contract check не проходит
- **WHEN** любой существующий contract check завершается с failure
- **THEN** mass runner сохраняет run и завершает верхнеуровневую команду с non-zero

### Requirement: Ошибки orchestration
Mass runner SHALL завершаться с non-zero, если каталог `eval/scenarios` отсутствует, не содержит каталогов `scenario-*`, невозможно создать run или дочерний process, отсутствует обязательный run artifact, любой scenario имеет failed automated checks либо contract checks не прошли. Он SHALL завершаться с zero только когда все scenarios и contract checks прошли automated validation.

#### Scenario: Scenarios не найдены
- **WHEN** `eval/scenarios` не содержит каталогов `scenario-*`
- **THEN** mass runner сообщает ошибку и завершается с non-zero

#### Scenario: Один scenario не создал result
- **WHEN** model subprocess завершается без run-local `result.json`
- **THEN** mass runner отмечает scenario как failed, продолжает доступные проверки и завершает верхнеуровневую команду с non-zero

#### Scenario: Все automated checks проходят
- **WHEN** каждый scenario имеет zero failed checks и contract checker завершается успешно
- **THEN** верхнеуровневая команда завершается с zero

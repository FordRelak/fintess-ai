## MODIFIED Requirements

### Requirement: README описывает контракт поведения
README scenario SHALL описывать проверяемые наблюдения, границы выводов и ожидаемые действия анализатора. README SHALL описывать несколько допустимых вариантов run-local `result.json` и SHALL NOT дублировать точные matcher-правила из `ground-truth.json`.

#### Scenario: Допустим альтернативный валидный результат
- **WHEN** run-local результат анализа отличается формулировками от README, но проходит правила `ground-truth.json`
- **THEN** README не описывает это отличие как ошибку scenario

### Requirement: Scenario-001 документирован
Repository SHALL содержать `eval/scenarios/scenario-001/README.md`, созданный по общему шаблону. README SHALL описывать локальное плато `smith-incline-bench-press`, прогрессию остальных упражнений, недопустимость общепрограммного вывода и запуск общего harness.

#### Scenario: Читатель воспроизводит проверку scenario-001
- **WHEN** читатель открывает `eval/scenarios/scenario-001/README.md`
- **THEN** он видит назначение каждого source и run-local artifact и команду запуска `RunHarness.cs`

### Requirement: Роли evaluation-артефактов разделены
Документация scenario SHALL определять raw fixtures как model-independent source data, `ground-truth.json` как источник точных automated acceptance rules, а `metrics.json`, `result.json` и `evaluation.json` как run-local generated artifacts. Документация scenario SHALL ссылаться на `eval/result-authoring-guide.md` как общую инструкцию автора `result.json`. Статус `manual_review` SHALL быть описан как допустимый при отсутствии `failed` automated checks.

#### Scenario: Проверка завершилась manual review без failed checks
- **WHEN** evaluator возвращает `manual_review` и не содержит checks со статусом `failed`
- **THEN** README описывает результат как прошедший automated checks с необходимостью semantic review

### Requirement: Документация граничных сценариев
Repository SHALL документировать scenarios `002`--`008` по общему README template. Каждый README SHALL указывать основной pattern, допустимые conclusions, недопустимые claims и команду общего harness.

#### Scenario: Читатель открывает новый scenario
- **WHEN** читатель открывает README любого scenario `002`--`008`
- **THEN** он видит, какую аналитическую границу проверяют fixtures, почему альтернативный вывод запрещён и как запустить suite

### Requirement: Общая проверка scenarios
README template SHALL содержать команду `RunHarness.cs` для проверки всех scenarios и SHALL описывать, что generated outputs создаются только в run-local paths и не изменяют scenario inputs.

#### Scenario: Contributor проверяет весь suite
- **WHEN** contributor использует README template для проверки suite
- **THEN** он видит единственную команду общего runner и условие успеха при `manual_review` без failed checks

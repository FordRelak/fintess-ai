## ADDED Requirements

### Requirement: Набор граничных сценариев
Repository SHALL содержать scenarios от `scenario-002-insufficient-history` до `scenario-008-sustained-performance-decline`. Каждый scenario SHALL содержать raw fixtures, generated `metrics.json`, один валидный `result.json`, `ground-truth.json`, `evaluation.json` и README.

#### Scenario: Scenario изолирует одну аналитическую границу
- **WHEN** contributor открывает любой scenario от `002` до `008`
- **THEN** его README и fixtures определяют одну основную границу classification, а ground truth запрещает противоречащие conclusions

### Requirement: Проверка snapshots сценариев
Repository SHALL предоставлять runner, который пересоздаёт normalized metrics и evaluator output для каждого каталога `scenario-*` во temporary paths, сравнивает их с committed snapshots и завершает работу с non-zero при drift, schema error или failed automated check.

#### Scenario: Committed snapshot отличается
- **WHEN** пересозданный `metrics.json` или `evaluation.json` отличается от committed counterpart
- **THEN** runner сообщает scenario и завершает работу с non-zero, не перезаписывая fixture

#### Scenario: Evaluation требует только semantic review
- **WHEN** evaluator result содержит ноль failed checks и один или несколько manual-review checks
- **THEN** runner считает scenario прошедшим automated verification

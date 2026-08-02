## MODIFIED Requirements

### Requirement: Набор граничных сценариев
Repository SHALL содержать scenarios от `scenario-002-insufficient-history` до `scenario-008-sustained-performance-decline`. Каждый source scenario SHALL содержать raw fixtures, `ground-truth.json` и README и SHALL NOT содержать generated `metrics.json`, `result.json` или `evaluation.json`.

#### Scenario: Scenario изолирует одну аналитическую границу
- **WHEN** contributor открывает любой scenario от `002` до `008`
- **THEN** его README, raw fixtures и ground truth определяют одну основную границу classification, а ground truth запрещает противоречащие conclusions

### Requirement: Проверка generated outputs сценариев
Repository SHALL использовать общий `RunHarness.cs` для model generation и evaluation всех каталогов `scenario-*`. Runner SHALL создавать `metrics.json`, `result.json` и `evaluation.json` только в отдельном run directory и SHALL завершаться с non-zero при missing artifact или failed automated check.

#### Scenario: Generated output invalid
- **WHEN** run-local `metrics.json` или `result.json` отсутствует либо evaluator содержит failed checks
- **THEN** runner сообщает scenario и завершает работу с non-zero, не изменяя source scenario

#### Scenario: Evaluation требует только semantic review
- **WHEN** evaluation содержит ноль failed checks и один или несколько manual-review checks
- **THEN** runner считает scenario прошедшим automated verification

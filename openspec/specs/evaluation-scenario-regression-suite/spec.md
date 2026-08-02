## Purpose

Изолированные evaluation scenarios и проверка generated outputs.

## Requirements

### Requirement: Набор граничных сценариев
Repository SHALL содержать scenarios от `scenario-002-insufficient-history` до `scenario-008-sustained-performance-decline`. Каждый scenario SHALL содержать raw fixtures, один валидный `result.json`, `ground-truth.json` и README. Generated `metrics.json` и `evaluation.json` SHALL храниться только внутри run или temporary directory.

#### Scenario: Scenario изолирует одну аналитическую границу
- **WHEN** contributor открывает любой scenario от `002` до `008`
- **THEN** его README и fixtures определяют одну основную границу classification, а ground truth запрещает противоречащие conclusions

### Requirement: Проверка generated outputs сценариев
Repository SHALL предоставлять runner, который пересоздаёт normalized metrics и evaluator output для каждого каталога `scenario-*` во temporary paths, проверяет JSON, schema и failed automated checks и завершает работу с non-zero при ошибке. Runner SHALL NOT требовать committed `metrics.json` или `evaluation.json`.

#### Scenario: Generated output invalid
- **WHEN** пересозданный `metrics.json` или `evaluation.json` невалиден либо evaluator содержит failed checks
- **THEN** runner сообщает scenario и завершает работу с non-zero, не изменяя scenario inputs

#### Scenario: Evaluation требует только semantic review
- **WHEN** evaluator result содержит ноль failed checks и один или несколько manual-review checks
- **THEN** runner считает scenario прошедшим automated verification

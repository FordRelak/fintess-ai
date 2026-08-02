## MODIFIED Requirements

### Requirement: Контракт результата версии 1.1
Repository SHALL определять `eval/schemas/result-schema.json` как контракт `schemaVersion` `1.1`. Каждый run-local `result.json`, создаваемый domain skill и проверяемый evaluator, MUST объявлять `schemaVersion` `1.1`; raw inputs и `metrics.json` SHALL сохранять независимый version contract. Source scenarios SHALL NOT хранить result fixtures.

#### Scenario: Run-local result использует версию 1.1
- **WHEN** evaluator читает generated `result.json` из harness run
- **THEN** он принимает result только когда `schemaVersion` равен `1.1`

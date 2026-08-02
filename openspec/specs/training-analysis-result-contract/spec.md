## Purpose

Версионированный контракт результата анализа тренировочного прогресса.

## Requirements

### Requirement: Контракт результата версии 1.1
Repository SHALL определять `eval/schemas/result-schema.json` как контракт `schemaVersion` `1.1`. Каждый run-local `result.json`, создаваемый domain skill и проверяемый evaluator, MUST объявлять `schemaVersion` `1.1`; raw inputs и `metrics.json` SHALL сохранять независимый version contract. Source scenarios SHALL NOT хранить result fixtures.

#### Scenario: Run-local result использует версию 1.1
- **WHEN** evaluator читает generated `result.json` из harness run
- **THEN** он принимает result только когда `schemaVersion` равен `1.1`

### Requirement: Наблюдение performance decline
Result contract SHALL разрешать observation type `performance_decline` для устойчивого ухудшения сопоставимого результата на уровне упражнения. Decline observation MUST использовать scope `exercise` и MUST содержать его `exerciseId`.

#### Scenario: Устойчивое снижение представлено
- **WHEN** упражнение имеет минимум четыре сопоставимых последовательных performance с устойчивым ухудшением
- **THEN** result может содержать exercise-level observation `performance_decline`

### Requirement: Evidence программы с указанием упражнения
Result contract SHALL разрешать evidence item внутри program-scope observation указывать исходное упражнение через `exerciseId`. Observation-level `exerciseId` MUST присутствовать при scope `exercise` и MUST NOT присутствовать при scopes `program` и `body_weight`.

#### Scenario: Program plateau указывает участвующие упражнения
- **WHEN** result описывает общепрограммное plateau, подтверждённое несколькими упражнениями
- **THEN** его program-scope evidence может указать каждый `exerciseId`, не присваивая `exerciseId` самому observation

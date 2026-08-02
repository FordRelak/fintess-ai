## Purpose

Человекочитаемая инструкция по созданию валидного `result.json`.

## Requirements

### Requirement: Guide автора result.json
Repository SHALL хранить `eval/result-authoring-guide.md` как человекочитаемую инструкцию для автора `result.json`. Guide SHALL описывать роли `observations`, `hypotheses`, `recommendations` и `limitations`, связи между ними, семантику допустимых enum-значений, ключевые структурные ограничения и проверочный список.

#### Scenario: Автор создаёт результат
- **WHEN** автор открывает guide перед созданием `result.json`
- **THEN** он видит, какие утверждения относятся к фактам, гипотезам, действиям и ограничениям, а также как выбрать значения schema

### Requirement: Schema является источником формата
Guide и skill SHALL ссылаться на `eval/schemas/result-schema.json` как единственный источник формального JSON-контракта. Repository MUST NOT хранить независимую копию этой schema в skill.

#### Scenario: Изменяется контракт результата
- **WHEN** меняются допустимые поля или enum-значения `result.json`
- **THEN** изменение вносится в `eval/schemas/result-schema.json`, а guide и skill используют этот же файл

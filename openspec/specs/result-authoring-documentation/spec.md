## Purpose

Человекочитаемая инструкция по созданию валидного `result.json`.

## Requirements

### Requirement: Guide автора result.json
Repository SHALL хранить `eval/result-authoring-guide.md` как человекочитаемую инструкцию для автора `result.json`. Guide SHALL описывать роли `observations`, `hypotheses`, `recommendations` и `limitations`, связи между ними, семантику всех допустимых enum-значений, включая `performance_decline`, ключевые структурные ограничения и проверочный список для result contract `1.1`. Guide и skill SHALL представлять подтверждённое plateau каждого существенного упражнения отдельным exercise-level observation; если те же данные подтверждают plateau всей программы, они SHALL добавлять отдельный program-level observation, не заменяя локальные observations. Program-level observation MUST NOT иметь верхнеуровневый `exerciseId` и SHALL содержать отдельное evidence с `exerciseId` для каждого упражнения, на котором основан общий вывод. Все ссылки на observation IDs в hypotheses и recommendations SHALL указывать на существующие observations текущего результата.

#### Scenario: Автор создаёт результат
- **WHEN** автор открывает guide перед созданием `result.json`
- **THEN** он видит, какие утверждения относятся к фактам, гипотезам, действиям и ограничениям, а также как выбрать значения schema

### Requirement: Guide различает decline и похожие закономерности
`eval/result-authoring-guide.md` SHALL описывать `performance_decline` как устойчивое ухудшение сопоставимого performance и SHALL отличать его от plateau, progression, ожидаемого rep reset после повышения нагрузки и роста volume от добавления рабочих подходов.

#### Scenario: Падение reps после повышения веса
- **WHEN** reps снижаются непосредственно после увеличения внешней нагрузки
- **THEN** guide указывает, что этот факт сам по себе не подтверждает `performance_decline` или plateau

### Requirement: Guide описывает разреженные evidence
Guide SHALL указывать, что короткая или разреженная история не подтверждает устойчивую performance classification и может требовать limitations `insufficient_history` и `measurement_sparsity`.

#### Scenario: История содержит редкие выполнения
- **WHEN** exercise имеет несколько выполнений с большими календарными пропусками
- **THEN** guide предписывает не делать уверенный вывод о progression, plateau или decline только по этим точкам

### Requirement: Scope локального program target deviation
Guide и skill SHALL использовать scope `exercise` с `exerciseId` для `program_target_deviation`, относящегося к одному упражнению. Они SHALL использовать scope `program` только для системного отклонения нескольких упражнений или тренировок.

#### Scenario: Один exercise отклоняется от program context
- **WHEN** число подходов одного exercise устойчиво превышает его target `programContext`
- **THEN** result использует exercise-level `program_target_deviation` с этим `exerciseId`

#### Scenario: Локальные plateau образуют plateau программы
- **WHEN** метрики подтверждают plateau каждого существенного упражнения и общий plateau программы
- **THEN** результат содержит отдельные exercise-level observations и дополнительный program-level observation с exercise-specific evidence для каждого включённого упражнения

#### Scenario: Общий вывод не подтверждает причину
- **WHEN** result связывает hypothesis или recommendation с exercise-level и program-level observations
- **THEN** каждая ссылка указывает на существующий observation ID, а hypothesis остаётся отделённой от наблюдаемых evidence

### Requirement: Schema является источником формата
Guide и skill SHALL ссылаться на `eval/schemas/result-schema.json` как единственный источник формального JSON-контракта. Repository MUST NOT хранить независимую копию этой schema в skill.

#### Scenario: Изменяется контракт результата
- **WHEN** меняются допустимые поля или enum-значения `result.json`
- **THEN** изменение вносится в `eval/schemas/result-schema.json`, а guide и skill используют этот же файл

## MODIFIED Requirements

### Requirement: Guide автора result.json
Repository SHALL хранить `eval/result-authoring-guide.md` как человекочитаемую инструкцию для автора `result.json`. Guide SHALL описывать роли `observations`, `hypotheses`, `recommendations` и `limitations`, связи между ними, семантику допустимых enum-значений, ключевые структурные ограничения и проверочный список. Guide и skill SHALL представлять подтверждённое plateau каждого существенного упражнения отдельным exercise-level observation; если те же данные подтверждают plateau всей программы, они SHALL добавлять отдельный program-level observation, не заменяя локальные observations. Program-level observation MUST NOT иметь верхнеуровневый `exerciseId` и SHALL содержать отдельное evidence с `exerciseId` для каждого упражнения, на котором основан общий вывод. Все ссылки на observation IDs в hypotheses и recommendations SHALL указывать на существующие observations текущего результата.

#### Scenario: Автор создаёт результат
- **WHEN** автор открывает guide перед созданием `result.json`
- **THEN** он видит, какие утверждения относятся к фактам, гипотезам, действиям и ограничениям, а также как выбрать значения schema

#### Scenario: Локальные plateau образуют plateau программы
- **WHEN** метрики подтверждают plateau каждого существенного упражнения и общий plateau программы
- **THEN** результат содержит отдельные exercise-level observations и дополнительный program-level observation с exercise-specific evidence для каждого включённого упражнения

#### Scenario: Общий вывод не подтверждает причину
- **WHEN** result связывает hypothesis или recommendation с exercise-level и program-level observations
- **THEN** каждая ссылка указывает на существующий observation ID, а hypothesis остаётся отделённой от наблюдаемых evidence

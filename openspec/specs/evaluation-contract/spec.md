## Purpose

Контракт ground truth и evaluator для проверки результатов анализа тренировочного прогресса.

## Requirements

### Requirement: Обязательное evidence использует явную trend policy
Evaluation contract SHALL сопоставлять каждый обязательный элемент `evidence` только с явно объявленными значениями `trend` в `ground-truth` сценария. Для `evidence` `workingSetCount`, описывающего неизменное количество сопоставимых рабочих подходов, policy SHALL принимать `stable` и `consistent`.

#### Scenario: Observation плато с consistent количеством подходов проходит проверку
- **КОГДА** result содержит observation `performance_plateau` для `smith-incline-bench-press` с `trend` `consistent` для `workingSetCount`
- **И** все остальные обязательные поля observation и `evidence` соответствуют `scenario-001`
- **ТОГДА** evaluator отмечает check обязательного observation плато как `passed`

#### Scenario: Неподдерживаемый trend остаётся отклонённым
- **КОГДА** result содержит обязательное `evidence` `workingSetCount` с `trend`, отсутствующим в policy сценария
- **ТОГДА** evaluator не сопоставляет это `evidence` с обязательным observation

### Requirement: Confidence insufficient evidence отражает неопределённость
Evaluation contract SHALL разрешать `low`, `medium` и `high` `confidence` для гипотез типа `insufficient_evidence`. Выбранный `confidence` SHALL оставаться явным и SHALL NOT выводиться или повышаться evaluator.

#### Scenario: Insufficient evidence с low confidence проходит policy
- **КОГДА** result содержит гипотезу с типом `insufficient_evidence` и `confidence` `low`
- **ТОГДА** evaluator отмечает её hypothesis policy check как `passed`

#### Scenario: Confidence гипотезы не переписывается
- **КОГДА** result содержит гипотезу `insufficient_evidence` с любым разрешённым policy значением `confidence`
- **ТОГДА** evaluator проверяет объявленный `confidence`, не изменяя result

### Requirement: Структурные инварианты evaluator
Evaluator SHALL отклонять result, чей evidence interval выходит за границы parent observation interval. Evaluator SHALL отклонять ground-truth matchers с невалидными week ranges или несовместимыми scope и exercise selectors.

#### Scenario: Evidence выходит за интервал observation
- **WHEN** evidence item начинается до `onsetWeek` или заканчивается после `throughWeek` своего observation
- **THEN** evaluator завершает проверку с ошибкой до matching ground truth

### Requirement: Сопоставление разных обязательных observations
Evaluator SHALL сопоставлять каждое required observation с отдельным actual observation. Он SHALL поддерживать пустой список `requiredObservations` для scenarios, которым не нужен положительный performance observation.

#### Scenario: Одно observation не закрывает два упражнения
- **WHEN** ground truth требует observations для двух разных упражнений
- **THEN** одно actual observation не может удовлетворить оба requirement

#### Scenario: Insufficient history не имеет required observation
- **WHEN** ground truth содержит пустой список `requiredObservations`
- **THEN** evaluator валидирует остальные policies без schema failure

### Requirement: Сопоставление evidence программы
Ground truth evidence matcher SHALL поддерживать optional `exerciseId`, чтобы требовать evidence, относящееся к конкретному упражнению в program-scope observation.

#### Scenario: Program evidence указывает все упражнения с plateau
- **WHEN** ground truth требует program-level plateau evidence для нескольких exercise IDs
- **THEN** evaluator проходит только когда program observation содержит matching evidence для каждого required exercise

### Requirement: Evaluation остаётся строгим и ограниченным сценарием
Evaluator SHALL использовать точное сопоставление со значениями, объявленными в `ground-truth`, и SHALL NOT добавлять неявные `aliases`, fuzzy matching или глобально разрешать несвязанные комбинации `trend` и `confidence`. Evaluation schemas SHALL использовать версию `1.1` и SHALL включать `performance_decline` как observation type.

#### Scenario: Несвязанная комбинация confidence остаётся отклонённой
- **КОГДА** гипотеза использует комбинацию type и `confidence`, не объявленную в policy сценария
- **ТОГДА** evaluator отмечает её hypothesis policy check как `failed`

#### Scenario: Изменение policy сценария не меняет manual review
- **КОГДА** failed automated checks для `scenario-001` устранены
- **ТОГДА** существующие semantic claim checks сохраняют статус `manual_review`

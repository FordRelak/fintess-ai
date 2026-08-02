## ADDED Requirements

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

## MODIFIED Requirements

### Requirement: Evaluation остаётся строгим и ограниченным сценарием
Evaluator SHALL использовать точный matching со значениями из scenario `ground-truth` и SHALL NOT добавлять неявные aliases, fuzzy matching или глобально разрешать несвязанные combinations `trend` и `confidence`. Evaluation schemas SHALL использовать версию `1.1` и SHALL включать `performance_decline` как observation type.

#### Scenario: Несвязанная комбинация confidence остаётся отклонённой
- **WHEN** гипотеза использует комбинацию type и `confidence`, не объявленную в policy сценария
- **THEN** evaluator отмечает её hypothesis policy check как `failed`

#### Scenario: Изменение policy сценария не меняет manual review
- **WHEN** failed automated checks для `scenario-001` устранены
- **THEN** существующие semantic claim checks сохраняют статус `manual_review`

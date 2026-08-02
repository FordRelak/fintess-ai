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

### Requirement: Evaluation остаётся строгим и ограниченным сценарием
Evaluator SHALL использовать точное сопоставление со значениями, объявленными в `ground-truth`, и SHALL NOT добавлять неявные `aliases`, fuzzy matching или глобально разрешать несвязанные комбинации `trend` и `confidence`.

#### Scenario: Несвязанная комбинация confidence остаётся отклонённой
- **КОГДА** гипотеза использует комбинацию type и `confidence`, не объявленную в policy сценария
- **ТОГДА** evaluator отмечает её hypothesis policy check как `failed`

#### Scenario: Изменение policy сценария не меняет manual review
- **КОГДА** failed automated checks для `scenario-001` устранены
- **ТОГДА** существующие semantic claim checks сохраняют статус `manual_review`

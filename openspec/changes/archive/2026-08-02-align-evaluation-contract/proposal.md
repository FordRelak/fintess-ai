## Почему

Scenario `scenario-001` правильно распознаёт локальное плато, но evaluation завершается со статусом `failed` из-за рассогласования допустимых значений между результатом анализа и `ground-truth`. Контракт нужно выровнять сейчас, чтобы проверки отличали реальные ошибки анализа от валидных формулировок `evidence` и гипотез.

## Что изменится

- Разрешить `consistent` как допустимый `trend` для стабильного количества рабочих подходов в обязательном observation.
- Разрешить `low` `confidence` для гипотезы типа `insufficient_evidence`.
- Зафиксировать правила сопоставления `evaluator` в спецификации capability.
- Не добавлять неявные `aliases` и не ослаблять общий `matcher` evaluator.

## Capabilities

### Новые capabilities

- `evaluation-contract`: Контракт ground truth и evaluator для проверки результатов анализа тренировочного прогресса.

### Изменяемые capabilities

## Влияние

- `eval/scenarios/scenario-001/ground-truth.json` изменит acceptance policy.
- Для `eval/scripts/Evaluate.cs` проверяется сохранение строгого сопоставления без новых `aliases`.
- Появится capability-спецификация для будущих evaluation-сценариев.
- После изменений `scenario-001` должен перейти из `failed` в `manual_review`, если checks `manual_review` не меняются и failed checks исчезают.

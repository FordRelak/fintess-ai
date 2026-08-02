## 1. Согласовать policy сценария

- [x] 1.1 Обновить `eval/scenarios/scenario-001/ground-truth.json`: принимать `stable` и `consistent` для обязательного `evidence` `workingSetCount`.
- [x] 1.2 Обновить `eval/scenarios/scenario-001/ground-truth.json`: разрешить `low`, `medium` и `high` `confidence` для гипотез `insufficient_evidence`.

## 2. Проверить поведение evaluation

- [x] 2.1 Запустить evaluator для `scenario-001` и подтвердить, что `required-observation-01` проходит.
- [x] 2.2 Подтвердить, что `hypothesis-policy-01` проходит без изменения `result.json`.
- [x] 2.3 Подтвердить, что checks forbidden observation и forbidden recommendation не падают.
- [x] 2.4 Подтвердить, что semantic claim checks сохраняют `manual_review`, а общий статус равен `manual_review`.
- [x] 2.5 Проверить, что `eval/scripts/Evaluate.cs` не изменён и строгое сопоставление сохранено.

## 3. Проверить регрессии

- [x] 3.1 Запустить все доступные evaluation-сценарии и подтвердить отсутствие регрессий в несвязанных сценариях.
- [x] 3.2 Проверить summary сгенерированного `evaluation.json` и зафиксировать оставшиеся элементы `manual_review`.

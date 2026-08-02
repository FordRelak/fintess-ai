## MODIFIED Requirements

### Requirement: Роли evaluation-артефактов разделены
Документация scenario SHALL определять `ground-truth.json` как источник точных automated acceptance rules, `result.json` как пример валидного ответа, а `evaluation.json` как снимок конкретного запуска. Документация scenario SHALL ссылаться на `eval/result-authoring-guide.md` как общую инструкцию автора `result.json`. Статус `manual_review` SHALL быть описан как допустимый при отсутствии `failed` automated checks.

#### Scenario: Проверка завершилась manual review без failed checks
- **WHEN** evaluator возвращает `manual_review` и не содержит checks со статусом `failed`
- **THEN** README описывает результат как прошедший automated checks с необходимостью semantic review

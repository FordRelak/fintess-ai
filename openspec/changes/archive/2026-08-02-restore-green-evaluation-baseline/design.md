## Context

`scenario-007-program-wide-plateau` описывает шесть недель стабильных результатов в трёх упражнениях. Текущий `result.json` не сопоставляется ни с тремя обязательными exercise-level observations, ни с program-level observation, поэтому generated `evaluation.json` содержит четыре failed checks. Формальные ограничения задают `eval/schemas/result-schema.json`, evaluator и `ground-truth.json`; authoring behavior задают skill и guide. При генерации нового результата действует analysis boundary: анализатор читает только `metrics.json`, schema и guide, но не существующие `result.json`, `evaluation.json` или `ground-truth.json`.

## Goals / Non-Goals

**Goals:**

- Получить три exercise-level plateau observations и одно program-level plateau observation.
- Добавить в program observation отдельное evidence для `barbell-squat`, `barbell-bench-press` и `barbell-row`.
- Сохранить ссылочную целостность IDs между observations, hypotheses и recommendations.
- Пересоздать generated artifacts штатным skill и evaluator.
- Вернуть зелёный baseline всех scenarios и contract checks без ослабления acceptance rules.

**Non-Goals:**

- Изменение schema, evaluator, `ground-truth.json` или архитектуры harness.
- Добавление CI, общей verification-команды или исправление абсолютных путей.
- Архивирование активных OpenSpec changes.
- Ручное редактирование `evaluation.json`.

## Decisions

### Диагностировать контракт отдельно от чистой генерации

Сначала сравнить scenario inputs, acceptance rules, schema, evaluator behavior, skill и активные OpenSpec changes, чтобы установить причины failed checks и drift IDs. Перед authoring удалить старый `result.json` и запустить `analyze-training-progress` в разрешённой границе только по `metrics.json`, schema и guide. Это сохраняет `ground-truth.json` источником acceptance rules, но не подсказывает генератору конкретный ответ.

Альтернатива: исправить существующий JSON вручную по ground truth. Она быстрее, но нарушает analysis boundary и не проверяет, способен ли skill воспроизвести корректный результат.

### Представлять program plateau как дополнительный observation

Program-level plateau дополняет, а не заменяет exercise-level observations. Каждый локальный observation содержит собственное evidence; program observation не содержит верхнеуровневый `exerciseId`, но включает не менее одного evidence item с `exerciseId` для каждого упражнения, на котором основан общий вывод.

Альтернатива: оставить только program observation. Она теряет локальные факты и не соответствует acceptance rules сценария.

### Исправлять authoring instructions только при воспроизводимой необходимости

Если текущий skill после чистого запуска снова объединяет observations или теряет exercise-specific evidence, внести минимальное обобщённое уточнение в skill и при необходимости guide. Не добавлять scenario-specific IDs, названия упражнений или специальные ветви для `scenario-007`.

Альтернатива: всегда менять skill. Она создаёт лишний regression risk, если дефект ограничен устаревшим generated artifact.

### Считать evaluator единственным создателем evaluation snapshot

После генерации `result.json` запустить `Evaluate.cs` для `scenario-007`; `evaluation.json` не редактировать вручную. Затем выполнить `VerifyAll.cs`, `VerifyContracts.cs` и strict OpenSpec validation. Diff должен содержать только необходимые authoring и generated artifact changes.

## Risks / Trade-offs

- [Skill остаётся неоднозначным для program plateau] Минимально уточнить общие правила и повторно сгенерировать scenario; проверить все snapshots.
- [Ground truth влияет на содержимое generated result] Разделить диагностику и authoring, удалить старый result перед чистым skill run и соблюдать analysis boundary.
- [Изменение IDs ломает ссылки] Проверить все `basedOn` и связанные поля schema validation и evaluator; не сохранять старые IDs ради совместимости.
- [Focused commands перезапишут snapshots] Перезаписывать только обязательные artifacts `scenario-007`; остальные scenarios проверять через `VerifyAll.cs`, который использует temporary outputs.
- [Manual review остаётся в evaluation] Считать scenario успешным при `failed: 0`; manual semantic claim checks допустимы по действующему контракту.

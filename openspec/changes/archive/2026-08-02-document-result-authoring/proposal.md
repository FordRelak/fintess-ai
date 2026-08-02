## Why

Автору `result.json` приходится восстанавливать смысл полей и допустимых значений из JSON Schema и текста skill. Это замедляет создание результатов и повышает риск смешать факт, гипотезу, рекомендацию и ограничение.

## What Changes

- Добавить единый человекочитаемый guide для авторов `result.json`.
- Связать guide с README шаблоном scenarios и `scenario-001`.
- Сделать `eval/schemas/result-schema.json` единственным источником JSON Schema, удалив копию из skill.
- Обновить skill, чтобы она ссылалась на общую schema и guide.

## Capabilities

### New Capabilities
- `result-authoring-documentation`: Человекочитаемый контракт для создания валидного и семантически обоснованного `result.json`.

### Modified Capabilities
- `evaluation-scenario-documentation`: Документация scenario ссылается на общий guide автора результата.

## Impact

- `eval/result-authoring-guide.md`
- `eval/scenarios/README.template.md`
- `eval/scenarios/scenario-001/README.md`
- `.opencode/skills/analyze-training-progress/SKILL.md`
- `.opencode/skills/analyze-training-progress/references/result-schema.json` удаляется
- `eval/schemas/result-schema.json` остаётся без изменения формата

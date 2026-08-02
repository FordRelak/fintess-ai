## Context

`result-schema.json` задаёт машинную валидность, а skill описывает метод анализа. Автор результата вынужден читать оба источника и вручную выводить смысл enum-значений. Та же schema продублирована в skill и `eval/schemas/`.

## Goals / Non-Goals

**Goals:**
- Дать автору `result.json` короткий guide с семантикой полей, связями и проверочным списком.
- Сохранить `eval/schemas/result-schema.json` единственным источником формата.
- Связать guide с template и README существующего scenario.

**Non-Goals:**
- Не менять JSON Schema, normalizer, evaluator и acceptance rules.
- Не документировать форматы raw input для автора `result.json`.
- Не превращать guide в альтернативный источник точных enum-значений.

## Decisions

### Единый guide в `eval/`

Guide хранится в `eval/result-authoring-guide.md`, рядом со schema и scenarios. Он объясняет контракт для всех scenarios, поэтому не должен находиться в каталоге конкретного scenario.

### Разделение ролей guide и schema

Schema остаётся нормативным машинным контрактом. Guide описывает значение типов, правила выбора и минимальные примеры; при расхождении приоритет у schema.

### Одна копия schema

Копия в `.opencode/skills/analyze-training-progress/references/` удаляется. Skill обращается к `eval/schemas/result-schema.json` и guide по абсолютным для workspace относительным путям.

## Risks / Trade-offs

- [Guide устареет при изменении schema] → Явно указать приоритет schema и ссылку на неё; обновлять оба файла в одном change.
- [Skill потеряет доступ к schema вне repository] → Skill предназначена для этого workspace; её входные сценарии уже используют repository-local `metrics.json`.
- [Scenario README начнут дублировать guide] → Template содержит только ссылку на guide.

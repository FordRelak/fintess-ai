## Purpose

Самодостаточная документация evaluation scenarios и разграничение ролей их артефактов.

## Requirements

### Requirement: Единый шаблон README scenario
Repository SHALL хранить общий шаблон в `eval/scenarios/README.template.md`. Шаблон SHALL содержать разделы для цели проверки, сюжета, данных, ожидаемого поведения, недопустимых утверждений, порядка проверки, критерия успеха и ролей файлов.

#### Scenario: Создание документации нового scenario
- **WHEN** в `eval/scenarios/` добавляется новый scenario
- **THEN** его README создаётся по `eval/scenarios/README.template.md` и содержит все обязательные разделы шаблона

### Requirement: README описывает контракт поведения
README scenario SHALL описывать проверяемые наблюдения, границы выводов и ожидаемые действия анализатора. README SHALL NOT объявлять `result.json` единственным допустимым ответом и SHALL NOT дублировать точные matcher-правила из `ground-truth.json`.

#### Scenario: Допустим альтернативный валидный результат
- **WHEN** результат анализа отличается формулировками от примера в `result.json`, но проходит правила `ground-truth.json`
- **THEN** README не описывает это отличие как ошибку scenario

### Requirement: Scenario-001 документирован
Repository SHALL содержать `eval/scenarios/scenario-001/README.md`, созданный по общему шаблону. README SHALL описывать локальное плато `smith-incline-bench-press`, прогрессию остальных упражнений, недопустимость общепрограммного вывода и воспроизводимый запуск normalizer с evaluator.

#### Scenario: Читатель воспроизводит проверку scenario-001
- **WHEN** читатель открывает `eval/scenarios/scenario-001/README.md`
- **THEN** он видит назначение каждого scenario-артефакта и команды запуска `Normalize.cs` и `Evaluate.cs`

### Requirement: Роли evaluation-артефактов разделены
Документация scenario SHALL определять `ground-truth.json` как источник точных automated acceptance rules, `result.json` как пример валидного ответа, а `evaluation.json` как снимок конкретного запуска. Статус `manual_review` SHALL быть описан как допустимый при отсутствии `failed` automated checks.

#### Scenario: Проверка завершилась manual review без failed checks
- **WHEN** evaluator возвращает `manual_review` и не содержит checks со статусом `failed`
- **THEN** README описывает результат как прошедший automated checks с необходимостью semantic review

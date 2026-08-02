## Зачем

Evaluation покрывает только один чистый сценарий: локальное плато одного упражнения при прогрессе остальных. Это не проверяет границы классификации, разреженные данные, ложную прогрессию от добавленных подходов и устойчивое ухудшение результата.

Нужен версионированный контракт, который выражает `performance_decline`, и небольшой набор независимых сценариев для безопасного развития skill.

## Что меняется

- **BREAKING** Поднять версию контрактов `result.json` и `ground-truth.json` с `1.0` до `1.1`.
- Добавить observation type `performance_decline` для устойчивого ухудшения сопоставимого результата.
- Усилить schema и evaluator: пустой набор required observations, exercise-specific evidence для program observations, валидные интервалы evidence и строгое сопоставление нескольких exercise observations.
- Уточнить normalizer для разреженных недель массы тела.
- Уточнить правила skill и guide: decline, plateau, progression, post-load rep reset, рост объёма от добавленных подходов и sparse history.
- Добавить сценарии `002`--`008`: недостаточная история, reset повторений после роста нагрузки, рост объёма от добавленных подходов, разреженная история, плато упражнений с собственным весом, общепрограммное плато и устойчивое снижение результата.
- Добавить общий runner для проверки committed snapshots всех сценариев.

## Возможности

### Новые возможности
- `training-analysis-result-contract`: Версионированный контракт результата, включая observation `performance_decline` и evidence для анализа уровня программы.
- `evaluation-scenario-regression-suite`: Изолированные сценарии анализа тренировок и единая проверка их snapshots.
- `raw-fixture-normalization`: Нормализация разреженных еженедельных измерений массы без ложного week-to-week delta.

### Изменённые возможности
- `evaluation-contract`: Ground truth и evaluator поддерживают result contract `1.1`, пустые required observations и точное evidence matching.
- `evaluation-scenario-documentation`: Документация сценариев описывает snapshot verification для расширенного набора.
- `result-authoring-documentation`: Guide описывает новую классификацию performance decline и границы интерпретации evidence.

## Влияние

- `eval/schemas/result-schema.json` и `eval/schemas/ground-truth-schema.json`.
- `eval/scripts/Evaluate.cs`, `eval/scripts/Normalize.cs` и новый runner в `eval/scripts/`.
- `.opencode/skills/analyze-training-progress/SKILL.md`, `eval/result-authoring-guide.md`, шаблон сценария и все scenario fixtures.
- Существующий `scenario-001` мигрирует на contracts `1.1`; raw fixtures и `metrics.json` сохраняют версию `1.0`.

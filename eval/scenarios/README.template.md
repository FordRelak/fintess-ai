# <Scenario name>

## Purpose

<Какое поведение анализатора проверяет scenario.>

## Story

<Контекст программы и существенный паттерн в данных.>

## Data

<Период, упражнения, доступные и отсутствующие данные.>

## Expected behavior

<Наблюдения, границы выводов и ожидаемые действия анализатора. Несколько формулировок результата могут быть валидны.>

## Invalid claims

<Выводы, которые не следуют из данных и не должны появляться в анализе.>

## Files

Общие правила создания `result.json` описаны в [Result Authoring Guide](../result-authoring-guide.md).

| File | Role |
| --- | --- |
| `program.json` | Контекст программы и целевые параметры упражнений. |
| `workouts.json` | Исходные записи тренировок. |
| `measurements.json` | Исходные измерения веса тела. |
| `metrics.json` | Нормализованные метрики, созданные из исходных данных. |
| `result.json` | Пример одного валидного результата анализа, не единственный допустимый ответ. |
| `ground-truth.json` | Источник точных automated acceptance rules. |
| `evaluation.json` | Снимок конкретного запуска evaluator. |

## Verification

```bash
dotnet run eval/scripts/Normalize.cs -- --scenario eval/scenarios/<scenario-directory>
dotnet run eval/scripts/Evaluate.cs -- --scenario eval/scenarios/<scenario-directory>
```

## Success criteria

<Какие observations, ограничения и действия должны пройти automated checks. `manual_review` без `failed` checks означает, что automated checks пройдены и требуется semantic review.>

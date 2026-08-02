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
| `metrics.json` | Run-local метрики, создаваемые из исходных данных. В scenario не хранится. |
| `result.json` | Run-local результат domain skill. В scenario не хранится. |
| `ground-truth.json` | Источник точных automated acceptance rules. |
| `evaluation.json` | Run-local результат evaluator. В scenario не хранится. |

Результаты model-run не записываются в scenario directory. `RunHarness.cs` генерирует metrics и сохраняет outputs в `.harness-runs/<skill-id>/<run-id>/scenarios/<scenario-id>`.

## Verification

```bash
dotnet run eval/scripts/RunHarness.cs -- --skill-id analyze-training-progress --skill-path .opencode/skills/analyze-training-progress --model <model-name>
```

## Success criteria

<Какие observations, ограничения и действия должны пройти automated checks. `RunHarness.cs` сохраняет каждый run отдельно, не изменяет scenario inputs и возвращает non-zero при failed scenario или evaluation. `manual_review` без `failed` checks означает, что automated checks пройдены и требуется semantic review.>

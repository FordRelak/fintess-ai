# Scenario 008: Sustained performance decline

## Purpose

Проверить `performance_decline` для устойчивого ухудшения сопоставимого результата.

## Story

Шесть последовательных тяг штанги на 80 кг содержат падение рабочих повторений с 33 до 19. Число подходов и effort не меняются.

## Data

Есть шесть еженедельных тренировок и измерения массы. Нет данных о восстановлении, питании, технике, боли и травмах.

## Expected behavior

- Выявить exercise-level `performance_decline` для `barbell-row`.
- Не объявлять plateau или progression.
- Предложить targeted action без подтверждения причины decline.

## Invalid claims

- Нельзя объяснять decline fatigue, recovery, nutrition, technique, pain или injury как установленным фактом.
- Нельзя считать decline следствием увеличения нагрузки: нагрузка не менялась.

## Files

Общие правила создания `result.json` описаны в [Result Authoring Guide](../../result-authoring-guide.md).

| File | Role |
| --- | --- |
| `program.json` | Контекст программы и целевые параметры. |
| `workouts.json` | Исходные записи тренировок. |
| `measurements.json` | Исходные измерения веса тела. |
| `metrics.json` | Run-local метрики, созданные из исходных данных. В scenario не хранится. |
| `result.json` | Один валидный результат анализа. |
| `ground-truth.json` | Automated acceptance rules. |
| `evaluation.json` | Run-local результат evaluator. В scenario не хранится. |

## Verification

```bash
dotnet run eval/scripts/RunHarness.cs -- --skill-id analyze-training-progress --skill-path .opencode/skills/analyze-training-progress --model <model-name>
dotnet run eval/scripts/VerifyAll.cs
```

## Success criteria

Automated checks должны требовать `performance_decline` и запрещать plateau и progression для тяги штанги. `RunHarness.cs` сохраняет outputs в `.harness-runs/` и не изменяет scenario inputs. `manual_review` без `failed` checks означает успешную automated validation.

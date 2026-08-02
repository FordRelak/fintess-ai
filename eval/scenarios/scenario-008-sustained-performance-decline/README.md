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
| `metrics.json` | Нормализованные метрики, созданные из исходных данных. |
| `result.json` | Один валидный результат анализа. |
| `ground-truth.json` | Automated acceptance rules. |
| `evaluation.json` | Snapshot конкретного запуска evaluator. |

## Verification

```bash
dotnet run eval/scripts/Normalize.cs -- --scenario eval/scenarios/scenario-008-sustained-performance-decline
dotnet run eval/scripts/Evaluate.cs -- --scenario eval/scenarios/scenario-008-sustained-performance-decline
dotnet run eval/scripts/VerifyAll.cs
```

## Success criteria

Automated checks должны требовать `performance_decline` и запрещать plateau и progression для тяги штанги. `manual_review` без `failed` checks означает успешную automated validation; `VerifyAll.cs` не перезаписывает committed snapshots.

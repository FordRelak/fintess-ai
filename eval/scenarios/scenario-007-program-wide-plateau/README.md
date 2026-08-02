# Scenario 007: Program-wide plateau

## Purpose

Проверить program-wide plateau, который опирается на отдельные plateau всех существенных упражнений.

## Story

Присед, жим лежа и тяга штанги шесть недель сохраняют нагрузку, повторения, подходы и effort. Тренировки регулярны.

## Data

Есть шесть полных тренировок и еженедельные измерения массы. Нет recovery, nutrition и technique data.

## Expected behavior

- Выявить local plateau для каждого из трех упражнений.
- Выявить program-level plateau с evidence-level `exerciseId` для всех трех упражнений.
- Предложить program-level action без подтверждения причины.

## Invalid claims

- Нельзя назвать fatigue, recovery, nutrition или technique подтвержденной причиной.
- Нельзя приписывать `exerciseId` самому program observation.

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
dotnet run eval/scripts/Normalize.cs -- --scenario eval/scenarios/scenario-007-program-wide-plateau
dotnet run eval/scripts/Evaluate.cs -- --scenario eval/scenarios/scenario-007-program-wide-plateau
dotnet run eval/scripts/VerifyAll.cs
```

## Success criteria

Automated checks должны подтвердить три local plateau, один program plateau и evidence каждого упражнения. `manual_review` без `failed` checks означает успешную automated validation; `VerifyAll.cs` не перезаписывает committed snapshots.

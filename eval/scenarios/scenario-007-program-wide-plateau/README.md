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
| `metrics.json` | Run-local метрики, созданные из исходных данных. В scenario не хранится. |
| `result.json` | Run-local результат domain skill. В scenario не хранится. |
| `ground-truth.json` | Automated acceptance rules. |
| `evaluation.json` | Run-local результат evaluator. В scenario не хранится. |

## Verification

```bash
dotnet run eval/scripts/RunHarness.cs -- --skill-id analyze-training-progress --skill-path .opencode/skills/analyze-training-progress --model <model-name>
```

## Success criteria

Automated checks должны подтвердить три local plateau, один program plateau и evidence каждого упражнения. `RunHarness.cs` сохраняет outputs в `.harness-runs/` и не изменяет scenario inputs. `manual_review` без `failed` checks означает успешную automated validation.

# Scenario 006: Bodyweight plateau

## Purpose

Проверить plateau упражнения с собственным весом и действие только для него.

## Story

Шесть еженедельных выполнений `pull-up` содержат одинаковые 27 рабочих повторений при трех подходах и сопоставимом effort. Вес тела растет.

## Data

Есть шесть тренировок и измерение веса в день каждой тренировки. Нет данных о восстановлении, фактическом питании и технике.

## Expected behavior

- Выявить exercise-level plateau `pull-up`, не decline и не progression.
- Учитывать сопоставимые подходы и effort; не трактовать рост веса тела как доказательство причины.
- Предложить targeted action для `pull-up`.

## Invalid claims

- Нельзя объявлять plateau всей программы.
- Нельзя подтверждать fatigue, recovery, nutrition или technique как причину.

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

Automated checks должны подтвердить локальное plateau и targeted recommendation. `RunHarness.cs` сохраняет outputs в `.harness-runs/` и не изменяет scenario inputs. `manual_review` без `failed` checks означает успешную automated validation.

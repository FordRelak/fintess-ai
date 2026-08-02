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
| `metrics.json` | Нормализованные метрики, созданные из исходных данных. |
| `result.json` | Один валидный результат анализа. |
| `ground-truth.json` | Automated acceptance rules. |
| `evaluation.json` | Snapshot конкретного запуска evaluator. |

## Verification

```bash
dotnet run eval/scripts/Normalize.cs -- --scenario eval/scenarios/scenario-006-bodyweight-plateau
dotnet run eval/scripts/Evaluate.cs -- --scenario eval/scenarios/scenario-006-bodyweight-plateau
dotnet run eval/scripts/EvaluateAll.cs
dotnet run eval/scripts/VerifyAll.cs
```

## Success criteria

Automated checks должны подтвердить локальное plateau и targeted recommendation. `EvaluateAll.cs` перезаписывает `evaluation.json` всех scenarios без проверки результатов; `VerifyAll.cs` не перезаписывает committed snapshots. `manual_review` без `failed` checks означает успешную automated validation.

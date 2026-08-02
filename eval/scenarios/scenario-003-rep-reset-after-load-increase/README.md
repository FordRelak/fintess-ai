# Scenario 003: Rep reset after load increase

## Purpose

Проверить, что падение повторений после роста нагрузки не классифицируется как plateau или decline.

## Story

Тяга блока растет с 50 до 55 кг на четвертой неделе. Повторения сразу сбрасываются, затем растут при новой нагрузке.

## Data

Шесть последовательных недель, одно упражнение, три неизменных рабочих подхода и измерение массы тела в каждый день тренировки.

## Expected behavior

Нужна `performance_progression` для `cable-row`. Plateau и decline запрещены.

## Invalid claims

- Нельзя считать ожидаемый reset повторений после повышения веса снижением результата.

## Files

Общие правила создания `result.json` описаны в [Result Authoring Guide](../../result-authoring-guide.md).

| File | Role |
| --- | --- |
| `program.json` | План программы и цели упражнения. |
| `workouts.json` | Исходные записи тренировок. |
| `measurements.json` | Исходные измерения веса тела. |
| `metrics.json` | Нормализованные метрики, созданные из исходных данных. |
| `result.json` | Один валидный результат анализа. |
| `ground-truth.json` | Automated acceptance rules. |
| `evaluation.json` | Snapshot конкретного запуска evaluator. |

## Verification

```bash
dotnet run eval/scripts/Normalize.cs -- --scenario eval/scenarios/scenario-003-rep-reset-after-load-increase
dotnet run eval/scripts/Evaluate.cs -- --scenario eval/scenarios/scenario-003-rep-reset-after-load-increase
dotnet run eval/scripts/VerifyAll.cs
```

## Success criteria

Evaluator требует progression и не находит failed checks. `manual_review` без failed checks допустим; `VerifyAll.cs` не перезаписывает committed snapshots.

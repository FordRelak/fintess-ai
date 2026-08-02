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

Evaluator требует progression и не находит failed checks. `RunHarness.cs` сохраняет outputs в `.harness-runs/` и не изменяет scenario inputs. `manual_review` без failed checks допустим.

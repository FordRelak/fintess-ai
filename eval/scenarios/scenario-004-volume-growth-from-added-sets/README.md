# Scenario 004: Volume growth from added sets

## Purpose

Проверить, что рост объема от добавленных подходов определяется как отклонение от target, а не performance progression.

## Story

На cable fly вес и повторения не меняются. С третьей недели добавлен третий рабочий подход при плане максимум два.

## Data

Пять последовательных недель, одно external-load упражнение и измерение массы тела в каждый день тренировки.

## Expected behavior

Нужно `program_target_deviation` для `cable-fly` и действие по объему. Performance progression запрещена.

## Invalid claims

- Нельзя считать добавленный подход доказательством улучшения результата.

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
dotnet run eval/scripts/Normalize.cs -- --scenario eval/scenarios/scenario-004-volume-growth-from-added-sets
dotnet run eval/scripts/Evaluate.cs -- --scenario eval/scenarios/scenario-004-volume-growth-from-added-sets
dotnet run eval/scripts/VerifyAll.cs
```

## Success criteria

Evaluator требует target deviation и не находит failed checks. `manual_review` без failed checks допустим; `VerifyAll.cs` не перезаписывает committed snapshots.

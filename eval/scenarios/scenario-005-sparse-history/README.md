# Scenario 005: Sparse history

## Purpose

Проверить ограничение вывода при редких выполнениях с большими календарными пропусками.

## Story

Leg press записан четыре раза за 12 недель. Нагрузка меняется, но между точками нет последовательной истории.

## Data

12 недель, четыре тренировки и четыре измерения массы тела, каждое в день тренировки. Остальные недели пусты.

## Expected behavior

Нужны `insufficient_history` и `measurement_sparsity`. Performance progression, plateau и decline запрещены.

## Invalid claims

- Нельзя выводить устойчивый performance trend из редких разнесенных точек.

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
dotnet run eval/scripts/Normalize.cs -- --scenario eval/scenarios/scenario-005-sparse-history
dotnet run eval/scripts/Evaluate.cs -- --scenario eval/scenarios/scenario-005-sparse-history
dotnet run eval/scripts/VerifyAll.cs
```

## Success criteria

Evaluator требует оба limitations и не находит failed checks. `manual_review` без failed checks допустим; `VerifyAll.cs` не перезаписывает committed snapshots.

# Scenario 002: Insufficient history

## Purpose

Проверить отказ от performance classification при двух выполнениях.

## Story

Жим в тренажере записан только в двух последовательных неделях. Второе выполнение лучше, но серии данных нет.

## Data

Две недели, одно external-load упражнение и по одному измерению массы тела в день тренировки.

## Expected behavior

Нужен limitation `insufficient_history`. Progression, plateau и decline не подтверждаются.

## Invalid claims

- Нельзя классифицировать два выполнения как устойчивую прогрессию, плато или снижение.

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
dotnet run eval/scripts/Normalize.cs -- --scenario eval/scenarios/scenario-002-insufficient-history
dotnet run eval/scripts/Evaluate.cs -- --scenario eval/scenarios/scenario-002-insufficient-history
dotnet run eval/scripts/EvaluateAll.cs
dotnet run eval/scripts/VerifyAll.cs
```

## Success criteria

Evaluator требует `insufficient_history` и не находит failed checks. `EvaluateAll.cs` перезаписывает `evaluation.json` всех scenarios без проверки результатов; `VerifyAll.cs` не перезаписывает committed snapshots. `manual_review` без failed checks допустим.

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
| `metrics.json` | Run-local метрики, созданные из исходных данных. В scenario не хранится. |
| `result.json` | Run-local результат domain skill. В scenario не хранится. |
| `ground-truth.json` | Automated acceptance rules. |
| `evaluation.json` | Run-local результат evaluator. В scenario не хранится. |

## Verification

```bash
dotnet run eval/scripts/RunHarness.cs -- --skill-id analyze-training-progress --skill-path .opencode/skills/analyze-training-progress --model <model-name>
```

## Success criteria

Evaluator требует `insufficient_history` и не находит failed checks. `RunHarness.cs` сохраняет outputs в `.harness-runs/` и не изменяет scenario inputs. `manual_review` без failed checks допустим.

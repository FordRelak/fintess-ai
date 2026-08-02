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
| `metrics.json` | Run-local метрики, созданные из исходных данных. В scenario не хранится. |
| `result.json` | Run-local результат domain skill. В scenario не хранится. |
| `ground-truth.json` | Automated acceptance rules. |
| `evaluation.json` | Run-local результат evaluator. В scenario не хранится. |

## Verification

```bash
dotnet run eval/scripts/RunHarness.cs -- --skill-id analyze-training-progress --skill-path .opencode/skills/analyze-training-progress --model <model-name>
```

## Success criteria

Evaluator требует target deviation и не находит failed checks. `RunHarness.cs` сохраняет outputs в `.harness-runs/` и не изменяет scenario inputs. `manual_review` без failed checks допустим.

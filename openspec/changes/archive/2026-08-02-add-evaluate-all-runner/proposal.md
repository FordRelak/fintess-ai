## Why

После массового создания `result.json` каждый scenario приходится оценивать отдельной командой. `VerifyAll.cs` не подходит: он дополнительно нормализует данные, сравнивает committed snapshots и проверяет результат.

## What Changes

- Добавить `EvaluateAll.cs` для последовательного запуска `Evaluate.cs` во всех каталогах `scenario-*`.
- Записывать результат каждого запуска в стандартный `evaluation.json` соответствующего scenario.
- Продолжать обработку после любого exit code дочернего evaluator.
- Ограничить собственные ошибки runner случаями orchestration: отсутствие scenario-каталогов или невозможность запустить дочерний процесс.
- Не читать generated `evaluation.json`, не агрегировать статусы, не запускать normalizer и не сравнивать snapshots.

## Capabilities

### New Capabilities
- `batch-evaluation-runner`: Массовый запуск evaluator для всех scenarios без дополнительной валидации или интерпретации результатов.

### Modified Capabilities

Нет.

## Impact

- Новый file-based C# script: `eval/scripts/EvaluateAll.cs`.
- Документация команд массовой оценки в `AGENTS.md` и шаблоне/README scenarios.
- Без новых зависимостей, схем или изменений `Evaluate.cs`.

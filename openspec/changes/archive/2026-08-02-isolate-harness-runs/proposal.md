## Why

Массовый harness сейчас создаёт результаты прямо в `eval/scenarios`, поэтому прерванные и параллельные запуски могут удалить или перезаписать committed-артефакты и оставить рабочее дерево в частично изменённом состоянии. Нужен воспроизводимый run boundary, который сохраняет входы, результаты и происхождение каждого запуска отдельно.

## What Changes

- Добавляется уникальная директория `.harness-runs/<skill-id>/<run-id>` для каждого запуска с копиями scenario inputs и всеми generated outputs.
- Добавляется `run.json` с ID и временем запуска, проверяемым domain skill, путём skill, Git-состоянием repository и моделью.
- Верхнеуровневый массовый запуск генерирует `result.json` и `evaluation.json` только внутри run directory, запускает evaluator и contract checks и агрегирует их exit codes.
- Failed runs сохраняются для диагностики; автоматическая очистка не добавляется.
- `.harness-runs/` исключается из Git.
- Старый orchestration skill удаляется; запуск выполняется напрямую через `RunHarness.cs`.

## Capabilities

### New Capabilities
- `isolated-harness-run`: Lifecycle уникального harness-run, его directory layout, metadata, копирование входов, сохранение outputs и защита исходного рабочего дерева.

### Modified Capabilities
- `batch-evaluation-runner`: Массовый runner становится верхнеуровневой проверкой run outputs, учитывает результаты scenarios и contract checks в итоговом exit code и не пишет committed snapshots.

## Impact

- Затрагиваются file-based C# scripts в `eval/scripts/`, включая массовую orchestration и contract verification.
- Удаляется лишний orchestration skill; domain skill и способ передачи scenario paths не меняются.
- Добавляется ignored runtime storage `.harness-runs/` и запись в `.gitignore`.
- Форматы `metrics.json`, `result.json`, `evaluation.json`, ground truth и логика `analyze-training-progress` не меняются.

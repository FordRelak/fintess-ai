## Context

`Evaluate.cs` создаёт или перезаписывает `evaluation.json` одного scenario и возвращает `0`, `2` или `3` по итогу его checks. `VerifyAll.cs` намеренно имеет другую роль: он запускает normalizer, пишет temporary outputs, сравнивает snapshots и читает evaluation summary.

После массовой генерации `result.json` нужен отдельный runner, который только запускает evaluator по всему набору и сохраняет результаты на стандартных путях.

## Goals / Non-Goals

**Goals:**
- Запускать `Evaluate.cs` для каждого непосредственного `scenario-*` в `eval/scenarios` в детерминированном порядке.
- Передавать каждому дочернему процессу `--scenario <directory>`, чтобы evaluator использовал стандартный output `evaluation.json`.
- Дождаться завершения каждого процесса и продолжить следующий независимо от его exit code.
- Вернуть non-zero только если runner не может обнаружить scenarios или создать дочерний процесс.

**Non-Goals:**
- Не запускать `Normalize.cs`.
- Не читать, сравнивать или валидировать `evaluation.json` после его создания.
- Не агрегировать statuses, counts или exit codes evaluator.
- Не менять поведение `Evaluate.cs`, схемы либо `VerifyAll.cs`.
- Не выполнять scenarios параллельно.

## Decisions

### Отдельный file-based script

Добавляется `eval/scripts/EvaluateAll.cs`, а не новый режим `VerifyAll.cs`. Так не смешиваются два контракта: массовая генерация outputs и regression verification committed snapshots.

Альтернатива с параметром у `VerifyAll.cs` отклонена: режимы имеют разные side effects и проверки.

### Последовательный запуск в лексикографическом порядке

Runner получает непосредственные каталоги `scenario-*`, сортирует пути ordinal-comparison и запускает evaluator по одному. Это даёт предсказуемый порядок stdout/stderr и исключает одновременную запись в repository.

Альтернатива с параллельным запуском отклонена: scenarios мало, а выгода не оправдывает недетерминированный вывод.

### Дочерние exit codes не интерпретируются

Runner запускает `dotnet run eval/scripts/Evaluate.cs -- --scenario <scenario>` и ожидает завершения. Он не читает exit code для принятия решения и не останавливает набор. Это сохраняет предназначение инструмента: инициировать evaluation, не анализировать его итог.

Альтернатива, где `2`/`3` считаются ошибками runner, отклонена: она превращает orchestration в проверку качества результатов.

### Собственные ошибки только orchestration

Отсутствие `eval/scenarios`, отсутствие каталогов `scenario-*` или невозможность создать process являются ошибками `EvaluateAll.cs` и дают non-zero. Output evaluator не перенаправляется, поэтому сохраняет собственную диагностику.

## Risks / Trade-offs

- [Один scenario может завершиться с ошибкой незаметно для shell exit code] Runner намеренно не суммирует exit codes; пользователь читает вывод `Evaluate.cs` либо запускает `VerifyAll.cs` для проверки.
- [Committed `evaluation.json` перезаписываются] Это цель команды; regression correctness остаётся обязанностью отдельного `VerifyAll.cs`.
- [Изменение текущего directory ломает relative paths] Скрипты repository уже требуют запуск из корня; документация явно фиксирует этот способ запуска.

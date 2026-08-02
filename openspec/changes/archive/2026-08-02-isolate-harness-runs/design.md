## Context

Harness состоит из file-based C# scripts и domain OpenCode skill. Ранее orchestration skill удалял committed `result.json`, запускал domain skill рядом с исходными `metrics.json`, а `EvaluateAll.cs` перезаписывал committed `evaluation.json` и игнорировал результат evaluator. Существующие `Evaluate.cs` и `VerifyContracts.cs` уже содержат нужную contract logic, но их orchestration привязана к scenario directories.

Изоляция должна охватывать весь запуск, включая AI subprocesses, evaluator и contract checks. Run должен существовать до запуска модели, чтобы частичный failure оставил диагностируемые входы и outputs. Repository может быть dirty или не иметь commit; эти состояния нельзя скрывать за вымышленным hash.

## Goals / Non-Goals

**Goals:**

- Дать одну file-based C# команду, которая создаёт run, запускает domain skill для всех scenarios, оценивает результаты, выполняет contract checks и возвращает агрегированный exit code.
- Обеспечить уникальный immutable run location для параллельных процессов.
- Сохранить generated `metrics.json`, `result.json`, `evaluation.json` и provenance запуска только в run.
- Не создавать, не удалять и не перезаписывать файлы в `eval/scenarios`.
- Сохранить failed runs без cleanup.

**Non-Goals:**

- Менять domain analysis rules, schemas, ground truth или evaluator matching.
- Повторно решать repository-root discovery или абсолютные пути skills.
- Добавлять CI, retention или поддержку других domain skills.
- Превращать repository в `.csproj`-based приложение.

## Decisions

### Одна команда владеет lifecycle

Добавляется `eval/scripts/RunHarness.cs`. Команда принимает явные `--skill-id`, `--skill-path` и `--model`, создаёт run, генерирует `metrics.json` из raw fixtures, запускает model process для каждого scenario, затем evaluator и contract checks. Она продолжает обработку независимых scenarios после локального failure, чтобы run содержал максимально полную диагностику, но в конце возвращает non-zero при любом failure.

Альтернатива с отдельными prepare/finalize scripts отклонена: interruption между командами усложняет ownership и не даёт единого aggregate exit code. Альтернатива оставить AI orchestration только внутри skill отклонена: skill invocation не предоставляет надёжный process exit code вызывающему shell.

### OpenCode запускает domain skill как subprocess

Runner использует установленный `opencode run`, передаёт выбранную модель и prompt с точным run-local `metrics.json`, domain `skill-id` и требованием записать `result.json` рядом с input. Каждый subprocess получает отдельный scenario directory. Runner не передаёт model process пути к ground truth, evaluation или committed result.

`--model` обязателен и записывается без преобразования в `run.json`. Это предпочтительнее автоматического определения активной модели, которого CLI надёжно не предоставляет, и не допускает двусмысленных metadata.

Альтернатива встроить model API в C# отклонена: она добавляет provider-specific dependency, credentials и model protocol в repository, где это уже решает OpenCode.

### Run ID резервируется атомарным созданием directory

`run-id` строится как UTC timestamp `yyyy-MM-dd'T'HH-mm-ss'Z'` и четыре lowercase hex символа от криптографического random source. Runner создаёт `.harness-runs/<skill-id>/<run-id>` через операцию, которая падает при существующем directory; при редкой коллизии генерирует новый suffix. Directory создаётся до записи metadata и запуска subprocesses.

Timestamp делает run читаемым, suffix исключает совместное использование directory параллельными процессами. PID отклонён: он повторно используется и не переносим между hosts.

### Run layout отделяет model-visible input от repository snapshots

Для каждого непосредственного `scenario-*` runner создаёт `scenarios/<scenario-id>`, генерирует туда `metrics.json` из raw fixtures, затем ожидает run-local `result.json` и пишет run-local `evaluation.json`. Evaluator получает run-local result и explicit read-only paths к committed `ground-truth.json` и schemas. Так domain skill видит только разрешённый input, а duplicate ground truth не появляется внутри model workspace.

Связь с exact evaluator inputs обеспечивается `repositoryCommit` и dirty-state metadata. Dirty repository явно помечается как невоспроизводимый без сохранения diff; snapshotting Git diff не входит в эту задачу.

### Metadata явно моделирует Git state

`run.json` записывается сразу после создания directory. Помимо обязательных `runId`, `skillId`, `skillPath`, `startedAt`, `repositoryCommit` и `model`, он содержит `repositoryDirty` и `repositoryState` со значениями `clean`, `dirty`, `unborn` или `unavailable`.

При clean/dirty repository `repositoryCommit` содержит фактический `HEAD`; dirty state не заменяет hash, а дополняет его. При unborn или недоступном Git `repositoryCommit` равен JSON `null`, а причина выражена `repositoryState`. Поле `startedAt` использует UTC ISO 8601 с секундами.

Отдельная JSON Schema для `run.json` не добавляется: формат локален runner и проверяется contract tests, а repository guide запрещает ненужное размножение contracts.

### Existing evaluator остаётся источником evaluation semantics

Runner вызывает `Evaluate.cs` с explicit `--scenario`, `--result`, `--ground-truth`, schema paths и `--output`. Exit code `0` считается passed; code `3` считается automated success только после чтения `evaluation.json` и подтверждения `summary.failed == 0`; остальные codes являются failure. Evaluation продолжается для оставшихся scenarios.

`VerifyContracts.cs` получает параметры для run root и source scenarios. Позитивные проверки читают run-local `result.json` и `metrics.json`; synthetic invalid variants остаются во temporary directory. Contract checker не удаляет run directory и возвращает non-zero при любой contract failure.

### Отдельный orchestration skill не нужен

`RunHarness.cs` вызывается напрямую с `--skill-id`, `--skill-path` и `--model`. Отдельный orchestration skill удалён, потому что не добавлял поведения поверх CLI.

## Risks / Trade-offs

- [Risk] `opencode` отсутствует или model process не может записать output. Mitigation: runner фиксирует уже созданный run, продолжает где возможно и возвращает non-zero с scenario-specific diagnostics.
- [Risk] Dirty repository нельзя точно воспроизвести только по commit hash. Mitigation: `repositoryDirty` и `repositoryState: dirty` делают ограничение явным; сохранение diff можно добавить отдельно.
- [Risk] Параллельные model subprocesses увеличивают нагрузку и усложняют logs. Mitigation: уникальные scenario directories исключают file races; runner связывает ошибки с scenario ID и агрегирует после ожидания всех процессов.
- [Risk] Failed run накапливает данные. Mitigation: намеренно сохранять все runs; retention вынесен за scope.
- [Trade-off] Run генерирует metrics из repository raw fixtures и читает ground truth и schemas из repository во время evaluation. Это сохраняет analysis boundary и компактный layout; commit и dirty metadata фиксируют provenance evaluator inputs.

## Migration Plan

1. Добавить `.harness-runs/` в `.gitignore` и реализовать runner с metadata и atomic run allocation.
2. Адаптировать batch evaluator и contract checks к run-local paths без изменения semantic logic.
3. Обновить документацию и удалить лишний orchestration skill.
4. Добавить contract tests для layout, dirty/unborn metadata, collision resistance, failed-run preservation, aggregate exit codes и неизменности scenario tree.
5. Выполнить existing verifier suite и новый isolated harness test. Rollback удаляет новый runner и возвращает прежний skill; уже созданные ignored runs можно оставить.

## Open Questions

Нет блокирующих вопросов. Поддержка дополнительных skill IDs потребует только нового orchestration adapter или параметров запуска, без изменения layout.

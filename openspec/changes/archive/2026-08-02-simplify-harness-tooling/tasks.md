## 1. Упрощение orchestration

- [x] 1.1 Удалить из `RunHarness.cs` post-run вызов contract checker и агрегацию его status.
- [x] 1.2 Удалить CLI options и code paths `--opencode`, `--result-source`, `--evaluate-script` и `--contract-script`, сохранив обязательные `--skill-id`, `--skill-path` и `--model`.
- [x] 1.3 Зафиксировать прямые вызовы `opencode` и repository `eval/scripts/Evaluate.cs`, сохранив run-local paths, logs, продолжение после scenario failure и обработку `manual_review`.

## 2. Удаление legacy tooling и fixtures

- [x] 2.1 Удалить `eval/scripts/VerifyAll.cs`, `eval/scripts/VerifyHarness.cs`, `eval/scripts/VerifyContracts.cs` и `eval/scripts/EvaluateAll.cs`.
- [x] 2.2 Удалить committed `result.json` из всех каталогов `eval/scenarios/scenario-*`.
- [x] 2.3 Проверить, что каждый source scenario содержит README, raw fixtures и `ground-truth.json`, но не содержит `metrics.json`, `result.json` или `evaluation.json`.

## 3. Документация единого flow

- [x] 3.1 Обновить `AGENTS.md` и `README.md`: оставить `RunHarness.cs` единственным массовым runner, убрать verifier/legacy commands и contract-stage wording.
- [x] 3.2 Обновить `eval/scenarios/README.template.md`: описать source artifacts, три run-local generated artifacts и одну команду общего harness.
- [x] 3.3 Обновить README всех scenarios по шаблону, убрав committed result example и команды удалённых scripts.
- [x] 3.4 Проверить актуальные repository docs на ссылки на удалённые scripts и committed scenario results, не изменяя `openspec/changes/archive/`.

## 4. Проверка изменения

- [x] 4.1 Запустить `dotnet run eval/scripts/RunHarness.cs -- --help`, `dotnet run eval/scripts/Normalize.cs -- --help` и `dotnet run eval/scripts/Evaluate.cs -- --help`, подтвердив компиляцию оставшихся entry points.
- [x] 4.2 Выполнить focused normalization одного scenario во временный output и проверить, что source scenario не изменился.
- [x] 4.3 Запустить `openspec validate --all --strict --no-interactive` и устранить ошибки актуальных artifacts.
- [x] 4.4 Проверить diff: generated outputs отсутствуют в scenarios, архивные OpenSpec changes не изменены, удалены только согласованные scripts и fixtures.

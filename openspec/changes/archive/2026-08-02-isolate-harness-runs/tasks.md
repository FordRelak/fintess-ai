## 1. Run Storage And Metadata

- [x] 1.1 Добавить `/.harness-runs/` в `.gitignore`.
- [x] 1.2 Создать `eval/scripts/RunHarness.cs` с CLI для обязательных `--skill-id`, `--skill-path` и `--model`, ordinal discovery scenarios и atomic allocation UTC-plus-random run ID.
- [x] 1.3 Реализовать раннюю запись `run.json` с requested model, repository-relative skill identity и явными clean, dirty, unborn и unavailable Git states.
- [x] 1.4 Реализовать генерацию каждого `metrics.json` из raw fixtures в run-local scenario directory без записи или удаления файлов в `eval/scenarios`.

## 2. Model And Evaluation Orchestration

- [x] 2.1 Добавить запуск `opencode run` для каждого run-local `metrics.json` с точным domain skill ID, выбранной моделью и требованием создать соседний `result.json`, не раскрывая model process evaluation inputs.
- [x] 2.2 Запускать `Evaluate.cs` с explicit run-local result/output и committed read-only ground truth/schema paths; корректно считать code `3` успешным только при `summary.failed == 0`.
- [x] 2.3 Продолжать независимые scenarios после model/evaluator failure, собирать scenario diagnostics и возвращать non-zero при отсутствующем result, invalid output или failed automated check.
- [x] 2.4 Адаптировать `VerifyContracts.cs` для проверки run-local results и metrics с сохранением synthetic mutations во temporary directory; включить его status в итоговый exit code.
- [x] 2.5 Гарантировать сохранение run directory и доступных artifacts при любом failure или interruption; не добавлять cleanup и retention.

## 3. Skill And Documentation Migration

- [x] 3.1 Удалить лишний `.opencode/skills/analyze-all-training-scenarios/SKILL.md`; запускать `RunHarness.cs` напрямую для `analyze-training-progress`.
- [x] 3.2 Обновить `AGENTS.md`, repository README и scenario README/template: документировать новую верхнеуровневую команду, run layout, metadata, exit semantics и отсутствие side effects в `eval/scenarios`.

## 4. Contract Verification

- [x] 4.1 Добавить deterministic subprocess test seam и contract checks для run ID format, metadata fields, copied inputs и полного successful run layout без обращения к реальной модели.
- [x] 4.2 Добавить проверки двух параллельных запусков: разные run directories, отсутствие cross-run overwrite и сохранение обоих результатов.
- [x] 4.3 Добавить failure checks для model, evaluator и contract errors: non-zero aggregate exit, продолжение остальных scenarios и сохранённый failed run.
- [x] 4.4 Проверить snapshot дерева `eval/scenarios` и `git status` до/после successful и failed harness runs, подтвердив отсутствие harness-generated worktree changes.
- [x] 4.5 Запустить `dotnet run eval/scripts/VerifyAll.cs`, `dotnet run eval/scripts/VerifyContracts.cs` и новый isolated harness contract suite.
- [x] 4.6 Запустить `openspec validate --all --strict --no-interactive`.

# Repository Guide

## Structure

- Это AI-harness, не приложение: package/project manifests и CI отсутствуют. Исполняемая часть — file-based C# scripts в `eval/scripts/`; запускайте их из корня через .NET 10 SDK без `.csproj`.
- `.opencode/skills/analyze-training-progress/SKILL.md` задаёт метод анализа. `eval/schemas/result-schema.json` — единственный формальный контракт `result.json`; не создавайте копию schema внутри skill.
- Scenario содержит raw fixtures (`program.json`, `workouts.json`, `measurements.json`), `ground-truth.json` и README. `metrics.json`, `result.json` и `evaluation.json` создаются только внутри run.
- Raw fixtures и generated `metrics.json` имеют `schemaVersion: "1.0"`; `result.json` и `ground-truth.json` — `"1.1"`; `evaluation.json` — `"1.0"`. README создавайте по `eval/scenarios/README.template.md`.

## Analysis Boundary

- При создании `result.json` читайте только run-local `metrics.json`, затем schema и `eval/result-authoring-guide.md`. Не читайте соседние `ground-truth.json`, `evaluation.json` или существующий `result.json`: это утечка ожидаемого ответа.

## Verification

- Изолированный массовый harness: `dotnet run eval/scripts/RunHarness.cs -- --skill-id analyze-training-progress --skill-path .opencode/skills/analyze-training-progress --model <model-name>`. Создаёт outputs в `.harness-runs/<skill-id>/<run-id>` и возвращает non-zero при failed scenario или evaluation.
- Один scenario анализируется через общий `RunHarness.cs`; generated outputs не записываются в source scenario.
- Generated outputs создаются только во временных или run-local paths. Scenario inputs не перезаписываются.
- `Evaluate.cs` возвращает `0` для `passed`, `2` для `failed`, `3` для допустимого `manual_review`. Код `3` успешен только если `evaluation.json.summary.failed` равен `0`; `RunHarness.cs` обрабатывает это правило.
- После изменения OpenSpec artifacts: `openspec validate --all --strict --no-interactive`.

## Harness Runs

- Каждый run содержит `run.json` и `scenarios/<scenario-id>/{metrics,result,evaluation}.json`.
- `run.json` содержит `runId`, `skillId`, `skillPath`, `startedAt`, `model`, `repositoryCommit`, `repositoryDirty` и `repositoryState`.
- Failed runs сохраняются в `.harness-runs/`; cleanup и retention не выполняются.

## Change Rules

- При изменении raw fixtures, `ground-truth.json`, schemas или evaluator запустите полный `RunHarness.cs` с выбранной моделью; generated outputs в scenarios не коммитятся.
- Evaluator использует exact matching из `ground-truth.json`; не добавляйте fuzzy matching, неявные aliases или глобальные послабления для исправления одного scenario.
- JSON Schema не проверяет целостность `basedOn` и все интервальные/scope-инварианты; окончательной проверкой служит `Evaluate.cs`.
- OpenSpec prose пишется по-русски, но structural headers (`## ADDED Requirements`, `### Requirement:`, `#### Scenario:`) и normative keywords (`SHALL`, `MUST`, `SHALL NOT`, `MUST NOT`) остаются на английском.

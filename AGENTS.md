# Repository Guide

## Structure

- Это AI-harness, не приложение: package/project manifests и CI отсутствуют. Исполняемая часть — file-based C# scripts в `eval/scripts/`; запускайте их из корня через .NET 10 SDK без `.csproj`.
- `.opencode/skills/analyze-training-progress/SKILL.md` задаёт метод анализа. `eval/schemas/result-schema.json` — единственный формальный контракт `result.json`; не создавайте копию schema внутри skill.
- Scenario содержит raw fixtures (`program.json`, `workouts.json`, `measurements.json`), `result.json` и `ground-truth.json`. `metrics.json` и `evaluation.json` создаются только внутри run.
- Raw fixtures и generated `metrics.json` имеют `schemaVersion: "1.0"`; `result.json` и `ground-truth.json` — `"1.1"`; `evaluation.json` — `"1.0"`. README создавайте по `eval/scenarios/README.template.md`.

## Analysis Boundary

- При создании `result.json` читайте только run-local `metrics.json`, затем schema и `eval/result-authoring-guide.md`. Не читайте соседние `ground-truth.json`, `evaluation.json` или существующий `result.json`: это утечка ожидаемого ответа.

## Verification

- Полный regression suite: `dotnet run eval/scripts/VerifyAll.cs`. Он генерирует metrics и evaluation во временном каталоге, проверяет JSON и failed checks, не изменяя scenarios.
- Contract edge cases: `dotnet run eval/scripts/VerifyContracts.cs`.
- Один scenario: `dotnet run eval/scripts/Normalize.cs -- --scenario eval/scenarios/<scenario-directory>`; затем `dotnet run eval/scripts/Evaluate.cs -- --scenario eval/scenarios/<scenario-directory>`.
- Изолированный массовый harness: `dotnet run eval/scripts/RunHarness.cs -- --skill-id analyze-training-progress --skill-path .opencode/skills/analyze-training-progress --model <model-name>`. Создаёт outputs в `.harness-runs/<skill-id>/<run-id>` и возвращает non-zero при failed scenario или contract check.
- Legacy `EvaluateAll.cs` работает с committed scenario paths и не используется для изолированных model runs.
- Generated outputs создаются только во временных или run-local paths. Scenario inputs не перезаписываются.
- `Evaluate.cs` возвращает `0` для `passed`, `2` для `failed`, `3` для допустимого `manual_review`. Код `3` успешен только если `evaluation.json.summary.failed` равен `0`; `VerifyAll.cs` обрабатывает это правило.
- После изменения OpenSpec artifacts: `openspec validate --all --strict --no-interactive`.

## Harness Runs

- Каждый run содержит `run.json` и `scenarios/<scenario-id>/{metrics,result,evaluation}.json`.
- `run.json` содержит `runId`, `skillId`, `skillPath`, `startedAt`, `model`, `repositoryCommit`, `repositoryDirty` и `repositoryState`.
- Failed runs сохраняются в `.harness-runs/`; cleanup и retention не выполняются.

## Change Rules

- При изменении raw fixtures, `result.json`, `ground-truth.json`, schemas или evaluator запустите оба C# verifier; generated metrics и evaluation в scenarios не коммитятся.
- Evaluator использует exact matching из `ground-truth.json`; не добавляйте fuzzy matching, неявные aliases или глобальные послабления для исправления одного scenario.
- JSON Schema не проверяет целостность `basedOn` и все интервальные/scope-инварианты; окончательной проверкой служит `Evaluate.cs`.
- OpenSpec prose пишется по-русски, но structural headers (`## ADDED Requirements`, `### Requirement:`, `#### Scenario:`) и normative keywords (`SHALL`, `MUST`, `SHALL NOT`, `MUST NOT`) остаются на английском.

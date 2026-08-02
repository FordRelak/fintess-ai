# Repository Guide

## Structure

- Это AI-harness, не приложение: package/project manifests и CI отсутствуют. Исполняемая часть — file-based C# scripts в `eval/scripts/`; запускайте их из корня через .NET 10 SDK без `.csproj`.
- `.opencode/skills/analyze-training-progress/SKILL.md` задаёт метод анализа. `eval/schemas/result-schema.json` — единственный формальный контракт `result.json`; не создавайте копию schema внутри skill.
- Raw fixtures (`program.json`, `workouts.json`, `measurements.json`) и generated `metrics.json` имеют `schemaVersion: "1.0"`; `result.json` и `ground-truth.json` — `"1.1"`; `evaluation.json` остаётся `"1.0"`.
- В scenario `metrics.json` генерируется normalizer; `result.json` — один допустимый ответ; `ground-truth.json` — точные acceptance rules; `evaluation.json` — snapshot запуска evaluator. README создавайте по `eval/scenarios/README.template.md`.

## Analysis Boundary

- При создании `result.json` читайте только соответствующий `metrics.json`, затем schema и `eval/result-authoring-guide.md`. Не читайте соседние `ground-truth.json`, `evaluation.json` или существующий `result.json`: это утечка ожидаемого ответа.
- Skill `analyze-all-training-scenarios` сначала удаляет все scenario `result.json`, затем параллельно пересоздаёт их и намеренно не валидирует. Используйте только когда явно нужен полный destructive regeneration.

## Verification

- Полный snapshot suite: `dotnet run eval/scripts/VerifyAll.cs`. Он должен запускаться из корня, генерирует файлы во временном каталоге и сравнивает JSON trees с committed `metrics.json` и `evaluation.json`.
- Contract edge cases: `dotnet run eval/scripts/VerifyContracts.cs`.
- Один scenario: `dotnet run eval/scripts/Normalize.cs -- --scenario eval/scenarios/<scenario-directory>`; затем `dotnet run eval/scripts/Evaluate.cs -- --scenario eval/scenarios/<scenario-directory>`.
- Массовая evaluation: `dotnet run eval/scripts/EvaluateAll.cs`. Скрипт последовательно перезаписывает `evaluation.json` всех `scenario-*`, но не запускает normalizer, не читает результаты и не проверяет snapshots.
- Эти focused-команды по умолчанию перезаписывают committed snapshots. Для проверки без записи передавайте `--output <temporary-file>`.
- `Evaluate.cs` возвращает `0` для `passed`, `2` для `failed`, `3` для допустимого `manual_review`. Код `3` успешен только если `evaluation.json.summary.failed` равен `0`; `VerifyAll.cs` обрабатывает это правило.
- После изменения OpenSpec artifacts: `openspec validate --all --strict --no-interactive`.

## Change Rules

- При изменении raw fixtures пересоздайте `metrics.json`; при изменении `result.json`, `ground-truth.json`, schemas или evaluator пересоздайте `evaluation.json`. Затем запустите оба C# verifier.
- Evaluator использует exact matching из `ground-truth.json`; не добавляйте fuzzy matching, неявные aliases или глобальные послабления для исправления одного scenario.
- JSON Schema не проверяет целостность `basedOn` и все интервальные/scope-инварианты; окончательной проверкой служит `Evaluate.cs`.
- OpenSpec prose пишется по-русски, но structural headers (`## ADDED Requirements`, `### Requirement:`, `#### Scenario:`) и normative keywords (`SHALL`, `MUST`, `SHALL NOT`, `MUST NOT`) остаются на английском.

## Why

Harness содержит несколько вспомогательных runners, test seams и committed `result.json`, которые дублируют основной model-run flow и увеличивают число поддерживаемых путей. На текущем этапе достаточно одного изолированного пути `Normalize.cs` + domain skill + `Evaluate.cs`; дополнительные regression и synthetic contract проверки можно вернуть при появлении конкретной потребности.

## What Changes

- **BREAKING** Удалить `VerifyAll.cs`, `VerifyHarness.cs`, `VerifyContracts.cs` и legacy `EvaluateAll.cs`.
- **BREAKING** Удалить post-run contract stage из `RunHarness.cs`; итоговый status определяется generation и evaluation каждого scenario.
- **BREAKING** Удалить test seams `--opencode`, `--result-source`, `--evaluate-script` и `--contract-script`; использовать стандартные `opencode` и `eval/scripts/Evaluate.cs`.
- **BREAKING** Удалить committed `result.json` из всех scenarios. `metrics.json`, `result.json` и `evaluation.json` становятся только run-local generated artifacts.
- Обновить scenario README, repository guidance и OpenSpec contracts под единый harness flow.
- Сохранить raw fixtures, `ground-truth.json`, result/ground-truth schemas, authoring guide, domain skill, normalizer и evaluator.

## Capabilities

### New Capabilities

Нет.

### Modified Capabilities

- `batch-evaluation-runner`: убрать contract stage, script overrides и fixture-copy path из массового запуска.
- `isolated-harness-run`: закрепить минимальный состав source scenarios и строго run-local lifecycle всех generated artifacts.
- `evaluation-scenario-regression-suite`: убрать committed result fixtures и отдельный regression runner.
- `evaluation-scenario-documentation`: описывать `result.json` как run-local output, а общий harness как единственный путь проверки.
- `training-analysis-result-contract`: применять result contract к run-local model output вместо committed scenario fixture.

## Impact

- Удаляемые scripts: `eval/scripts/VerifyAll.cs`, `eval/scripts/VerifyHarness.cs`, `eval/scripts/VerifyContracts.cs`, `eval/scripts/EvaluateAll.cs`.
- Изменяемая orchestration: `eval/scripts/RunHarness.cs`.
- Удаляемые fixtures: `eval/scenarios/scenario-*/result.json`.
- Обновляемая документация: `AGENTS.md`, `README.md`, `eval/scenarios/README.template.md`, README всех scenarios.
- Обновляемые OpenSpec capabilities перечислены выше; архивные changes остаются историческими и не изменяются.
- Исчезают deterministic harness runs без модели, synthetic negative checks evaluator/normalizer и автоматическая проверка параллельной изоляции run.

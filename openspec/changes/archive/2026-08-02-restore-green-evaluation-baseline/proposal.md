## Why

`scenario-007-program-wide-plateau` имеет четыре failed checks: сгенерированный анализ не представляет три локальных plateau и отдельное plateau программы в форме, требуемой действующим контрактом. Из-за этого baseline всего evaluation harness красный, а committed generated artifacts расходятся с ожидаемым поведением сценария.

## What Changes

- Уточнить правила authoring для одновременного представления exercise-level и program-level plateau без потери локальных observations.
- Требовать отдельное evidence с `exerciseId` каждого существенного упражнения внутри program-level observation.
- Обеспечить согласованные IDs и ссылки между observations, hypotheses и recommendations.
- Заново сгенерировать `result.json` для `scenario-007` и пересоздать `evaluation.json` существующим evaluator.
- Подтвердить отсутствие regressions полным snapshot suite, contract checks и strict OpenSpec validation.
- Не ослаблять schema, evaluator или `ground-truth.json` и не менять архитектуру harness.

## Capabilities

### New Capabilities

Нет.

### Modified Capabilities

- `result-authoring-documentation`: Правила authoring уточняют структуру локального и общепрограммного plateau, exercise-specific evidence и целостность ссылок результата.

## Impact

- `.opencode/skills/analyze-training-progress/SKILL.md` только при необходимости устранения причины неверной генерации.
- `eval/scenarios/scenario-007-program-wide-plateau/result.json` и `evaluation.json` как generated artifacts.
- `eval/result-authoring-guide.md` только если действующие указания недостаточны для однозначного результата.
- Verification: `eval/scripts/VerifyAll.cs`, `eval/scripts/VerifyContracts.cs` и `openspec validate --all --strict --no-interactive`.

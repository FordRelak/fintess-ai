## 1. Диагностика scenario-007

- [x] 1.1 Сопоставить raw fixtures и `metrics.json` scenario-007 с `ground-truth.json`, актуальной result schema, evaluator, contract checks, skill и активными OpenSpec changes, не используя generated artifacts как источники требований.
- [x] 1.2 Проверить текущие generated `result.json` и `evaluation.json` и зафиксировать причины четырёх failed required-observation checks и рассогласования observation IDs со ссылками hypotheses и recommendations.

## 2. Правила authoring

- [x] 2.1 Уточнить `eval/result-authoring-guide.md`: program-level plateau дополняет отдельные exercise-level plateau, содержит exercise-specific evidence и не имеет верхнеуровневого `exerciseId`.
- [x] 2.2 Минимально уточнить `.opencode/skills/analyze-training-progress/SKILL.md`, чтобы skill сохранял локальные observations, создавал отдельный program observation и выдавал целостные ссылки без scenario-specific логики.

## 3. Регенерация scenario-007

- [x] 3.1 Удалить устаревший `result.json` и заново создать его через `analyze-training-progress`, читая для authoring только `metrics.json`, `eval/schemas/result-schema.json` и `eval/result-authoring-guide.md`.
- [x] 3.2 Проверить в новом `result.json` три exercise-level plateau, отдельное program-level plateau, evidence каждого упражнения и существование всех IDs, на которые ссылаются hypotheses и recommendations.
- [x] 3.3 Запустить `dotnet run eval/scripts/Evaluate.cs -- --scenario eval/scenarios/scenario-007-program-wide-plateau` и убедиться, что сгенерированный `evaluation.json` имеет `summary.failed: 0`.

## 4. Regression verification

- [x] 4.1 Запустить `dotnet run eval/scripts/VerifyAll.cs` и подтвердить зелёные snapshots всех восьми scenarios без перезаписи остальных committed artifacts.
- [x] 4.2 Запустить `dotnet run eval/scripts/VerifyContracts.cs` и подтвердить прохождение contract edge cases.
- [x] 4.3 Запустить `openspec validate --all --strict --no-interactive` и устранить ошибки OpenSpec artifacts без архивирования changes.
- [x] 4.4 Проверить `git diff`, подтвердить воспроизводимость committed `result.json` и `evaluation.json` и оставить только изменения, нужные для зелёного baseline, без ослабления schema, evaluator или ground truth.

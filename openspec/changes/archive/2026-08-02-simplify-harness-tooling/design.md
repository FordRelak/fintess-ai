## Context

Текущий repository является file-based AI harness без project manifests и CI. Основной `RunHarness.cs` уже выполняет полный model-run для каждого scenario, но рядом существуют четыре дополнительных scripts с перекрывающимися ролями. `VerifyContracts.cs` встроен в каждый harness run и повторно вызывает evaluator на synthetic variants; остальные runners запускаются только вручную. Source scenarios дополнительно содержат committed `result.json`, хотя основной flow генерирует новый result в изолированной run directory.

Изменение сводит систему к одному runtime пути и одному набору source artifacts. Формальные result/ground-truth schemas, semantic matching evaluator, normalization и domain skill не меняются.

## Goals / Non-Goals

**Goals:**

- Оставить один массовый flow: `RunHarness.cs`, `Normalize.cs`, `opencode run`, `Evaluate.cs`.
- Хранить `metrics.json`, `result.json` и `evaluation.json` только внутри `.harness-runs/`.
- Удалить scripts и CLI seams, не используемые основным flow.
- Сохранить isolation, provenance, продолжение после локального scenario failure и корректную обработку `manual_review`.
- Согласовать repository guidance, scenario README и OpenSpec requirements с упрощённой архитектурой.

**Non-Goals:**

- Менять raw fixture, result, ground-truth или evaluation JSON contracts.
- Менять метод анализа в `analyze-training-progress`.
- Ослаблять matching policies `Evaluate.cs` или `ground-truth.json`.
- Добавлять новый test framework, CI или замену удаляемым regression scripts.
- Изменять архивные OpenSpec changes.

## Decisions

### 1. Единственный orchestration entry point

`RunHarness.cs` остаётся единственным массовым runner. Для каждого source scenario он создаёт run-local directory, вызывает `Normalize.cs`, запускает `opencode run` с выбранным domain skill и моделью, затем вызывает `Evaluate.cs` с explicit paths к run-local result/output и source ground truth.

Альтернатива: сохранить `VerifyAll.cs` как быстрый deterministic path. Отклонено, потому что path зависит от committed results, которые удаляются, и поддерживает вторую модель lifecycle artifacts.

### 2. Удаление post-run contract stage

`RunHarness.cs` больше не вызывает `VerifyContracts.cs` после scenarios. Финальный exit code агрегирует только failures normalization, model generation и scenario evaluation. `manual_review` остаётся успешным automated outcome при `evaluation.json.summary.failed == 0`.

Альтернатива: сохранить synthetic checks вне основного flow. Отклонено на текущем этапе из-за отсутствия CI и конкретной потребности в отдельном evaluator regression suite.

### 3. Удаление test seams

Из CLI `RunHarness.cs` удаляются `--opencode`, `--result-source`, `--evaluate-script` и `--contract-script`. Runner использует `opencode` и repository path `eval/scripts/Evaluate.cs`. Обязательные `--skill-id`, `--skill-path` и `--model` сохраняются как реальные параметры запуска и provenance.

Альтернатива: оставить overrides без callers. Отклонено, потому что они расширяют контракт runner и позволяют flow, который repository больше не документирует и не проверяет.

### 4. Source scenario не содержит generated analysis result

Из каждого `eval/scenarios/scenario-*` удаляется `result.json`. Source scenario содержит только README, `program.json`, `workouts.json`, `measurements.json` и `ground-truth.json`. Generated metrics, result и evaluation существуют только в `.harness-runs/<skill-id>/<run-id>/scenarios/<scenario-id>`.

Это устраняет возможность случайно использовать committed answer как model input и делает lifecycle всех outputs единообразным. `ground-truth.json` остаётся source acceptance contract и передаётся только evaluator.

### 5. Исторические artifacts не переписываются

Актуальные specs и repository docs обновляются. `openspec/changes/archive/` сохраняется без изменений, даже если исторические документы упоминают удаляемые scripts или committed results.

## Risks / Trade-offs

- [Нет deterministic full run без модели] Проверка всего suite требует доступной модели и имеет стоимость; это принято как следствие удаления `--result-source` и committed results.
- [Нет synthetic evaluator regression checks] Ошибки interval validation, matcher uniqueness или schema edge cases обнаружатся через реальные runs либо focused ручную проверку; отдельный suite можно вернуть при появлении regressions.
- [Нет автоматической проверки isolation concurrency] Уникальность run ID остаётся в реализации и спецификации, но без отдельного executable verifier.
- [Сложнее менять evaluator] Изменения evaluator потребуют focused проверки на реальном run artifact; документация должна явно указывать основной harness command.
- [Удаление публичных CLI options] Локальные внешние callers могут сломаться; repository не содержит таких callers, а изменение отмечено как breaking.

## Migration Plan

1. Упростить `RunHarness.cs`, удалив alternate result source, executable overrides и contract stage.
2. Удалить четыре вспомогательных scripts.
3. Удалить committed `result.json` из всех source scenarios.
4. Обновить repository и scenario documentation под run-only artifacts и единственный runner.
5. Обновить актуальные OpenSpec specs и выполнить strict validation.
6. Проверить компиляцию оставшихся scripts и выполнить focused normalization/evaluation там, где доступен run-local result; полный end-to-end test требует указанной пользователем модели.

Rollback выполняется возвратом удалённых scripts, CLI branches и fixtures из предыдущего Git revision вместе с соответствующими specs и документацией.

## Open Questions

Нет. Стоимость полного model run и потеря synthetic checks приняты явно.

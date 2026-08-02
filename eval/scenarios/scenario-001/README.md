# Scenario 001: Local Smith incline press plateau

## Purpose

Проверить, что анализатор отличает локальное плато в `smith-incline-bench-press` от состояния всей программы и предлагает действие только для проблемного упражнения.

## Story

16-недельная программа на гипертрофию в surplus. После повышения Smith incline press до 65 кг на неделе 9 нагрузка и рабочие повторения перестали улучшаться. В тот же период chest-supported row, cable fly и pull-up продолжают прогрессировать.

## Data

В scenario есть одна тренировка в каждую из 16 недель, три измерения веса тела в неделю и четыре упражнения. Средний вес тела растет с 80.0 до 81.6 кг. Данных о восстановлении, фактическом питании, технике, боли и травмах нет.

## Expected behavior

Анализатор должен:

- Выявить локальное плато `smith-incline-bench-press` примерно с недель 9-11 до недели 16: нагрузка стабильна, повторения колеблются без положительного тренда, число рабочих подходов и effort сопоставимы.
- Отметить прогрессию chest-supported row, cable fly и pull-up, а также последовательный рост веса тела и регулярность тренировок.
- Ограничить причинные выводы: доступных данных недостаточно, чтобы подтвердить усталость, восстановление, питание, технику, боль или травму как причину плато.
- Рекомендовать пересмотреть progression только для `smith-incline-bench-press`, сохранив сопоставимые рабочие подходы и effort. Для остальных прогрессирующих упражнений сохранить текущий план.

Run-local `result.json` может использовать разные формулировки и структуру аргументации, если проходит automated acceptance rules из `ground-truth.json`.

## Invalid claims

- Нельзя объявлять плато всей программы или всех упражнений.
- Нельзя подтверждать дефицит калорий, недостаточный surplus, усталость, восстановление, питание, технику, боль или травму как причину плато.
- Нельзя считать рост веса тела доказательством достаточного питания, белка или восстановления.
- Нельзя утверждать известные изменения времени отдыха или e1RM: этих данных нет.
- Нельзя назначать общий high-priority deload или high-priority review nutrition только по этому локальному плато.

## Files

Общие правила создания `result.json` описаны в [Result Authoring Guide](../../result-authoring-guide.md).

| File | Role |
| --- | --- |
| `program.json` | План программы, целевые диапазоны повторений, подходов и effort. |
| `workouts.json` | Исходные записи 16 тренировок. |
| `measurements.json` | Исходные измерения веса тела. |
| `metrics.json` | Run-local метрики, созданные normalizer из input JSON. В scenario не хранится. |
| `result.json` | Run-local результат domain skill. В scenario не хранится. |
| `ground-truth.json` | Источник точных automated acceptance rules и matcher-правил. |
| `evaluation.json` | Run-local результат evaluator. В scenario не хранится. |

## Verification

Запускать из корня repository:

```bash
dotnet run eval/scripts/RunHarness.cs -- --skill-id analyze-training-progress --skill-path .opencode/skills/analyze-training-progress --model <model-name>
```

`RunHarness.cs` генерирует metrics, result и evaluation в `.harness-runs/` и не изменяет scenario inputs.

## Success criteria

Automated checks должны подтвердить локальное плато жима, прогрессию остальных упражнений, тренд веса тела, регулярность тренировок, требуемые ограничения и упражнение-специфичную рекомендацию. Точные условия хранятся только в `ground-truth.json`.

Статус `manual_review` при отсутствии checks со статусом `failed` означает, что automated checks пройдены, а semantic claims требуют ручной проверки.

# Result Authoring Guide

Инструкция для автора `result.json`. Формальный контракт и полный список допустимых значений задаёт [result-schema.json](schemas/result-schema.json). При расхождении guide и schema приоритет у schema.

## Быстрый путь

1. Читайте только `metrics.json`; `ground-truth.json` и существующий `result.json` не являются входными данными анализа.
2. Запишите подтверждённые закономерности в `observations`.
3. Запишите возможные причины в `hypotheses` и свяжите их с observation.
4. Запишите минимальные обоснованные действия в `recommendations`.
5. Запишите недостающие данные, ограничивающие выводы, в `limitations`.
6. Проверьте JSON по schema и ссылки `basedOn`.

```text
metrics.json
    |
    +-- observations: что данные прямо показывают
    |       |
    |       +-- hypotheses: почему это могло произойти
    |       |       |
    |       |       +-- recommendations: что сделать дальше
    |       |
    |       +-- limitations: чего нельзя установить из данных
```

## Структура

Верхний уровень всегда содержит:

```json
{
  "schemaVersion": "1.0",
  "observations": [],
  "hypotheses": [],
  "recommendations": [],
  "limitations": []
}
```

`observation.id` имеет вид `obs-...`, `hypothesis.id` - `hyp-...`. Используйте устойчивые смысловые ID: `obs-smith-press-plateau`, а не порядковые номера.

| Раздел | Назначение | Не помещать сюда |
| --- | --- | --- |
| `observations` | Факты, прямо подтверждённые метриками | Непроверенные причины |
| `hypotheses` | Возможные объяснения наблюдений | Установленные факты без evidence |
| `recommendations` | Следующее действие, обоснованное observation или hypothesis | Выдуманные сведения о причине |
| `limitations` | Какие отсутствующие или редкие данные сужают вывод | Диагнозы |

## Observations

`type` определяет наблюдаемую закономерность:

| Значение | Выбирайте, когда |
| --- | --- |
| `performance_plateau` | Устойчивый блок без улучшений при сопоставимых условиях |
| `performance_progression` | Сопоставимый показатель результата устойчиво улучшается |
| `body_weight_trend` | Меняется средняя недельная масса тела |
| `training_consistency` | Регулярность подтверждена уникальными workout ID и охватом недель |
| `program_target_deviation` | Подходы, повторения или effort существенно и неоднократно расходятся с `programContext` |

`scope` задаёт объект наблюдения:

| Значение | Объект | `exerciseId` |
| --- | --- | --- |
| `exercise` | Одно упражнение | Обязателен |
| `program` | Вся программа | Не указывать |
| `body_weight` | Масса тела | Не указывать |

`confidence` отражает надёжность наблюдения: `low`, `medium`, `high`. `high` подходит только для полной истории и однозначного тренда.

Каждый элемент `evidence` описывает метрику внутри интервала observation.

| `metric` | Смысл |
| --- | --- |
| `maxWeightKg` | Максимальный внешний вес рабочего подхода |
| `totalReps` | Сумма повторений рабочих подходов |
| `volumeKg` | Внешний объём рабочих подходов в килограммах |
| `workingSetCount` | Число рабочих подходов |
| `effort` | Заявленное усилие рабочих подходов |
| `bodyWeightAverageKg` | Средняя недельная масса тела |
| `workoutCount` | Количество тренировок |

| `trend` | Смысл |
| --- | --- |
| `increasing` | Значение растёт |
| `decreasing` | Значение снижается |
| `stable` | Числовое значение не меняется или почти не меняется |
| `fluctuating` | Есть колебания без направленного тренда |
| `consistent` | Регулярно повторяется одна и та же структура или частота |

`stable` описывает значение, например 65 кг. `consistent` описывает регулярность, например три рабочих подхода каждую неделю. `volumeKg` - вспомогательное evidence, если число подходов или effort менялись.

## Hypotheses

Гипотеза обязана содержать `basedOn` с существующими `obs-*` ID. Она не доказывает причину.

| `type` | Смысл |
| --- | --- |
| `exercise_specific_adaptation` | Возможная адаптация к конкретному упражнению |
| `programming_constraint` | Возможное ограничение способа программирования |
| `fatigue_accumulation` | Возможное накопление усталости |
| `recovery_constraint` | Возможное ограничение восстановления |
| `nutrition_constraint` | Возможное ограничение питания |
| `technique_constraint` | Возможное ограничение техники |
| `insufficient_evidence` | Причину нельзя определить по доступным данным |

При отсутствии прямых данных о восстановлении, питании, усталости или технике используйте для соответствующей причины `low` confidence. Если причина неразличима, предпочитайте `insufficient_evidence`.

## Recommendations

`basedOn` ссылается на существующие `obs-*` или `hyp-*` ID. `priority` принимает `low`, `medium` или `high`.

| `type` | Действие |
| --- | --- |
| `adjust_progression` | Изменить критерий или шаг прогрессии |
| `modify_volume` | Изменить объём работы |
| `modify_intensity` | Изменить интенсивность или нагрузку |
| `deload` | Назначить разгрузку |
| `substitute_exercise` | Заменить упражнение |
| `maintain_current_plan` | Сохранить текущий план |
| `review_recovery` | Собрать или пересмотреть данные восстановления |
| `review_nutrition` | Собрать или пересмотреть данные питания |
| `collect_more_data` | Собрать недостающие данные |

При локальном плато начните с действия для этого упражнения и укажите `exerciseId`. Не назначайте общий `deload` или `review_nutrition` только из локального плато либо из `nutritionPhase: "surplus"`.

## Limitations

Добавляйте limitation, только если отсутствие данных влияет на анализ.

| `type` | Когда использовать |
| --- | --- |
| `missing_recovery_data` | Нет сна, готовности или восстановления |
| `missing_nutrition_intake_data` | Нет фактических энергии и белка |
| `missing_technique_data` | Нет данных о технике, амплитуде или видео |
| `missing_pain_or_injury_data` | Отсутствие данных о боли или травме существенно для вывода |
| `measurement_sparsity` | Измерения слишком редкие |
| `insufficient_history` | Истории недостаточно для устойчивого вывода |
| `other` | Другое значимое ограничение |

## Минимальный пример

```json
{
  "id": "obs-press-plateau",
  "type": "performance_plateau",
  "scope": "exercise",
  "exerciseId": "smith-incline-bench-press",
  "onsetWeek": 10,
  "throughWeek": 16,
  "confidence": "high",
  "evidence": [
    {
      "metric": "maxWeightKg",
      "fromWeek": 10,
      "throughWeek": 16,
      "trend": "stable"
    }
  ],
  "summary": "Нагрузка не менялась в течение семи последовательных выполнений."
}
```

## Checklist

- `schemaVersion` равно `"1.0"`.
- Все enum-значения есть в schema.
- Каждый `exercise` observation содержит `exerciseId`.
- `fromWeek <= throughWeek`; evidence не выходит за интервал observation.
- Каждый `basedOn` указывает на существующий ID.
- Факт не выдаётся за причину.
- Нет неизвестных полей и Markdown внутри JSON.

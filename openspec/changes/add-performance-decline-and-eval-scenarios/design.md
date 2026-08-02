## Контекст

`scenario-001` проверяет длинную, полную и однозначную историю: локальное плато Smith press, прогресс трёх остальных упражнений и рост массы тела. Он не проверяет, когда классификацию делать нельзя, и не выражает устойчивое ухудшение результата.

`result.json` и `ground-truth.json` сейчас имеют версию `1.0`; raw fixtures и нормализованный `metrics.json` также имеют собственный контракт `1.0`. Версионирование результата отделяется от raw contract: меняются только schemas, evaluator, result fixtures и ground truth.

## Цели и нецели

**Цели:**
- Ввести версионированные result и ground-truth contracts `1.1`.
- Выражать устойчивое ухудшение через `performance_decline`.
- Сделать evaluator точным для program-level evidence и нескольких required observations.
- Добавить небольшие сценарии, каждый из которых проверяет одну аналитическую развилку.
- Проверять committed `metrics.json` и `evaluation.json` snapshots одним runner.

**Нецели:**
- Не добавлять фактические данные сна, питания, боли или техники в `metrics.json`.
- Не превращать hypotheses о fatigue, recovery, nutrition, technique или injury в подтверждённые факты.
- Не менять raw fixture schema version и не вводить новую JSON Schema для `metrics.json` в этом change.
- Не менять алгоритм тренировки или назначать конкретные нагрузки.

## Решения

### Result и ground truth используют `1.1`

`result-schema.json` и `ground-truth-schema.json` будут требовать `schemaVersion: "1.1"`. `scenario-001` и новые `result.json`/`ground-truth.json` мигрируют одновременно. Raw `program.json`, `workouts.json`, `measurements.json` и generated `metrics.json` остаются `1.0`; `evaluation.json` также остаётся `1.0`, поскольку его output shape не меняется.

Это явная breaking boundary вместо неявного изменения смысла `1.0`.

### `performance_decline` описывает только устойчивый сопоставимый спад

Skill и guide определят decline как минимум четыре последовательных сопоставимых выполнения с устойчивым ухудшением weight или reps при сопоставимых sets и effort. Он исключает одно-два плохих выполнения, спад reps после роста веса и спад volume из-за уменьшения sets. Причины остаются hypotheses.

Альтернатива `performance_regression` отклонена: `performance_decline` согласуется с evidence trend `decreasing` и не подразумевает медицинскую причину.

### Evidence program observation получает `exerciseId`

`observation.exerciseId` остаётся только для exercise scope. Optional `evidence.exerciseId` разрешён только как атрибуция evidence конкретного упражнения внутри program-scope observation. Это позволяет program-wide plateau явно показать вклад каждого упражнения.

Альтернатива без exercise ID оставляет program observation без проверяемой связи с упражнениями.

### Evaluator валидирует структурные инварианты до matching

`Evaluate.cs` проверит вложенность evidence interval в observation interval, валидность всех range bounds, согласованность scope/exercise и уникальное потребление actual observation required matcher-ом. Matcher продолжит требовать точные type, scope, confidence, exercise и явно разрешённые evidence trends.

Пустой `requiredObservations` разрешён для insufficient-history scenarios. Это не означает пустой validation: forbidden observations, limitations, recommendations и claim policy продолжают проверяться.

### Scenarios остаются raw-first snapshots

Каждый сценарий содержит raw fixtures, generated `metrics.json`, пример `result.json`, `ground-truth.json`, `evaluation.json` и README. Новый runner генерирует outputs во временный каталог, сравнивает JSON trees с committed snapshots и допускает `manual_review` только при нуле failed checks.

Альтернатива с ручным запуском каждого scenario отклонена: она не защищает snapshots от drift.

### Sparse weekly delta не перескакивает пропуск

Если у предыдущей календарной недели нет среднего веса, normalizer записывает `changeFromPreviousWeekKg: null`. Поле сохраняет своё текущее имя и не создаёт ложный week-to-week trend через пропуск.

## Риски и компромиссы

- [Breaking schema version отклонит старые result fixtures] Мигрировать `scenario-001` и все новые result/ground-truth fixtures в одном change.
- [Большое число fixtures станет трудно читать] Каждый новый scenario ограничить одной аналитической развилкой и кратким README.
- [Program-wide plateau может быть чрезмерно сильным выводом] Требовать локальные plateau для всех существенных упражнений, регулярность тренировок и exercise-attributed evidence.
- [Snapshot runner может маскировать ошибку, если перезаписывает fixtures] Генерировать только temporary files и завершаться ошибкой при различии.
- [Sparse history может давать разные корректные ограничения] Ground truth проверяет обязательные safety boundaries, не единственную формулировку результата.

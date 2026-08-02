## 1. Контракты и evaluator

- [x] 1.1 Обновить `result-schema.json` до контракта `1.1`, добавить `performance_decline` и правила scope для exercise ID observation/evidence.
- [x] 1.2 Обновить `ground-truth-schema.json` до контракта `1.1`, добавить decline, разрешить пустые required observations и evidence matchers с указанием упражнения.
- [x] 1.3 Обновить структурную validation в `Evaluate.cs`: вложенность evidence, валидные диапазоны matcher, согласованность scope и разные required observations.
- [x] 1.4 Обновить matching evaluator для program evidence с указанием упражнения и мигрировать result/ground-truth fixtures `scenario-001` на `1.1`.
- [x] 1.5 Обновить `Normalize.cs`: weekly body-weight delta равен null при пропущенной предыдущей календарной неделе.

## 2. Правила анализа и tooling проверки

- [x] 2.1 Обновить skill `analyze-training-progress`: критерии decline, исключения, ограничения sparse history и canonical path result schema.
- [x] 2.2 Обновить `result-authoring-guide.md` для контракта `1.1`, семантики decline, post-load rep reset, added-set volume и sparse evidence.
- [x] 2.3 Обновить README template сценария: инструкции проверки всего набора.
- [x] 2.4 Добавить `VerifyAll.cs`: пересоздавать snapshots во temporary paths, сравнивать JSON с committed fixtures и падать при drift или failed checks.

## 3. Граничные сценарии

- [x] 3.1 Добавить `scenario-002-insufficient-history`: без performance classification, с обязательным insufficient-history limitation.
- [x] 3.2 Добавить `scenario-003-rep-reset-after-load-increase`: progression обязателен, plateau и decline запрещены.
- [x] 3.3 Добавить `scenario-004-volume-growth-from-added-sets`: program target deviation обязателен, ложная performance progression запрещена.
- [x] 3.4 Добавить `scenario-005-sparse-history`: ограничения sparse data обязательны, performance classifications запрещены.
- [x] 3.5 Добавить `scenario-006-bodyweight-plateau`: exercise-level bodyweight plateau с targeted action.
- [x] 3.6 Добавить `scenario-007-program-wide-plateau`: разные local plateaus, program evidence с указанием упражнения и program-level plateau.
- [x] 3.7 Добавить `scenario-008-sustained-performance-decline`: exercise-level decline, plateau и progression запрещены.

## 4. Проверка

- [x] 4.1 Пересоздать `metrics.json` и `evaluation.json` snapshots для scenarios `001`--`008`.
- [x] 4.2 Добавить и запустить negative contract checks: отклонение старой версии, принятие decline schema, невалидные evidence intervals, пустые required observations, разные matcher и sparse weekly delta.
- [x] 4.3 Запустить `VerifyAll.cs` и `openspec validate` для завершённого change.

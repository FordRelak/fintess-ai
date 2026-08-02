## 1. Массовый runner

- [x] 1.1 Добавить `eval/scripts/EvaluateAll.cs`: обнаружение и ordinal-сортировка непосредственных `scenario-*` в `eval/scenarios`.
- [x] 1.2 Запускать `Evaluate.cs` последовательно с `--scenario`, без `--output`, без перенаправления output и без интерпретации дочерних exit codes.
- [x] 1.3 Обработать orchestration errors: отсутствующий каталог scenarios, пустой набор и невозможность создать process.

## 2. Документация

- [x] 2.1 Добавить команду `dotnet run eval/scripts/EvaluateAll.cs` и её side effect в `AGENTS.md`.
- [x] 2.2 Обновить `eval/scenarios/README.template.md` и существующие scenario README: различить массовую генерацию `evaluation.json` через `EvaluateAll.cs` и snapshot verification через `VerifyAll.cs`.

## 3. Проверка

- [x] 3.1 Запустить `EvaluateAll.cs` и убедиться, что он последовательно создаёт outputs всех scenarios без post-evaluation проверки.
- [x] 3.2 Запустить `dotnet run eval/scripts/VerifyAll.cs` и `dotnet run eval/scripts/VerifyContracts.cs`.
- [x] 3.3 Выполнить `openspec validate add-evaluate-all-runner --strict --no-interactive`.

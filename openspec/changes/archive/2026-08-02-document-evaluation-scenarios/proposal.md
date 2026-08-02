## Why

Сценарий `scenario-001` содержит все данные для воспроизводимой проверки, но не объясняет человеку проверяемое поведение, границы допустимых выводов и порядок запуска. По мере добавления сценариев нужна единая документационная структура, чтобы каждый сценарий оставался понятным и самостоятельным.

## What Changes

- Добавить README для `scenario-001`, описывающий его цель, контекст, ожидаемое поведение анализатора, запрещённые выводы и воспроизводимый путь проверки.
- Добавить `eval/scenarios/README.template.md` как единый шаблон для README всех будущих evaluation scenarios.
- Зафиксировать, что README scenario создаётся по шаблону и описывает ожидаемое поведение, а не единственный эталонный `result.json`.
- Явно отделить документацию ожидаемого поведения от машиночитаемых acceptance rules в `ground-truth.json` и от примера валидного результата в `result.json`.

## Capabilities

### New Capabilities
- `evaluation-scenario-documentation`: Самодостаточная документация evaluation scenario с единым шаблоном и разграничением ролей артефактов.

### Modified Capabilities

Нет.

## Impact

- `eval/scenarios/scenario-001/README.md`
- `eval/scenarios/README.template.md`
- Возможный индекс scenarios в `eval/scenarios/`
- Будущие каталоги в `eval/scenarios/`
- Контракт `ground-truth.json`, `result.json`, `metrics.json` и scripts остаётся без изменений.

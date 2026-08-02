## ADDED Requirements

### Requirement: Документация граничных сценариев
Repository SHALL документировать scenarios `002`--`008` по общему README template. Каждый README SHALL указывать основной pattern, допустимые conclusions, недопустимые claims и команды локальной проверки.

#### Scenario: Читатель открывает новый scenario
- **WHEN** читатель открывает README любого scenario `002`--`008`
- **THEN** он видит, какую аналитическую границу проверяют fixtures и почему альтернативный вывод запрещён

### Requirement: Общая проверка snapshots
README template SHALL содержать команду общего runner для проверки всех scenario snapshots и SHALL описывать, что runner не перезаписывает committed fixtures.

#### Scenario: Contributor проверяет весь suite
- **WHEN** contributor использует README template для проверки suite
- **THEN** он видит команду запуска общего runner и условие успеха при `manual_review` без failed checks

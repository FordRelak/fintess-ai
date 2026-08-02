## ADDED Requirements

### Requirement: Delta разреженной еженедельной массы тела
Normalizer SHALL устанавливать `bodyWeight.weekly[].changeFromPreviousWeekKg` в `null`, когда текущая или непосредственно предыдущая календарная неделя не имеет `averageKg`. Он SHALL NOT сравнивать текущее weekly average с более ранней несмежной observed week через пропуск измерений.

#### Scenario: Предыдущая календарная неделя не имеет измерения
- **WHEN** неделя имеет average body weight, но непосредственно предыдущая календарная неделя имеет `averageKg: null`
- **THEN** normalizer выдаёт `changeFromPreviousWeekKg: null` для текущей недели

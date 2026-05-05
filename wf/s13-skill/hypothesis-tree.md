# Hypothesis Tree: Grade Notification Sends All Grades

## H1 — Frontend sends ALL non-empty grades, not just changed ones (быстро)
**Файл/функция:** `frontend/src/views/CourseProgress.vue`, функция `saveGrades()` (строки 183-196)
**Механизм:** `saveGrades()` собирает все записи из `grades.value`, фильтруя только пустые значения (`v !== ''`), и отправляет их все на сервер. При этом не сравнивается с `originalGrades.value` — т.е. отправляются ВСЕ заполненные оценки, включая те, что не менялись.
**Подтверждение:** В `saveGrades()` нет сравнения с `originalGrades` — фильтр только `v !== ''`.
**Опровержение:** Если бы фильтр сравнивал с `originalGrades.value`, отправлялись бы только изменённые.
**Стоимость:** Быстро — видно из чтения кода (уже подтверждено).

## H2 — Backend не фильтрует changed vs unchanged и передаёт все полученные записи в уведомление (быстро)
**Файл/функция:** `backend/Api/Controllers/CoursesController.cs`, метод `SaveGrades` (строка 308) → `NotifyGradesSavedSafeAsync`
**Механизм:** Backend принимает `dto.Grades` и передаёт их целиком в `NotifyGradesSavedSafeAsync(id, dto.Grades)`. Нет фильтрации по changed/unchanged.
**Подтверждение:** Строка 308: `NotifyGradesSavedSafeAsync(id, dto.Grades)` — передаётся весь список.
**Опровержение:** Если бы backend читал из БД только новые/изменённые записи и передавал их, проблема была бы на стороне БД.
**Стоимость:** Быстро — видно из чтения кода (уже подтверждено).

## H3 — GradeNotificationAdapter не фильтрует и рендерит все переданные записи (быстро)
**Файл/функция:** `backend/Api/Services/GradeNotificationAdapter.cs`, метод `BuildEmailBody` (строки 131-153)
**Механизм:** `BuildEmailBody` получает `List<GradeEntryDto>` и итерирует все записи без фильтрации.
**Подтверждение:** Цикл `foreach (var grade in grades)` на строке 142 — рендерит все.
**Опровержение:** Если бы была проверка на `IsNew` или `IsChanged`, рендерились бы только изменённые.
**Стоимость:** Быстро — видно из чтения кода (уже подтверждено).

## H4 — Frontend track changes (hasChanges) но не использует tracking для отправки (быстро)
**Файл/функция:** `frontend/src/views/CourseProgress.vue`, `markChanged()` и `saveGrades()`
**Механизм:** `markChanged()` (строка 147-149) сравнивает весь `grades.value` с `originalGrades.value` и устанавливает `hasChanges`, но `saveGrades()` (строка 186) игнорирует это и собирает все записи заново.
**Подтверждение:** `markChanged` используется только для disabled кнопки (строка 47), не для фильтрации payload.
**Опровержение:** Если бы `saveGrades` вычислял diff и отправлял только changed записи, проблема была бы решена.
**Стоимость:** Быстро — видно из чтения кода.

---

## Итоговая сортировка по стоимости проверки (все — быстро, т.к. код уже прочитан):

| # | Гипотеза | Стоимость | Статус |
|---|----------|-----------|--------|
| H1 | Frontend `saveGrades()` отправляет все non-empty записи без diff | Быстро | Подтверждена |
| H2 | Backend передаёт весь `dto.Grades` в уведомления | Быстро | Подтверждена |
| H3 | `BuildEmailBody` рендерит все переданные записи | Быстро | Подтверждена |
| H4 | `hasChanges` не используется для фильтрации payload | Быстро | Подтверждена |

## Первичная причина
**H1** — корневая проблема: frontend функция `saveGrades()` не вычисляет diff между `grades.value` и `originalGrades.value`, а отправляет все non-empty записи. Все последующие компоненты (backend controller, notification adapter) работают корректно в рамках полученного payload. Исправление должно быть на стороне frontend — отправлять только изменённые записи.

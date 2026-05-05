# Research Results: Grade Notification Sends All Grades

## Date
2026-05-05

## Hypotheses Verification

### H1 — Frontend `saveGrades()` отправляет все non-empty записи без diff
**Файл:** `frontend/src/views/CourseProgress.vue`, строки 183-196
**Факты:**
- `saveGrades()` собирает entries из `Object.entries(grades.value)`
- Фильтр: только `v !== ''` (непустые значения)
- Нет сравнения с `originalGrades.value`
- `originalGrades` обновляется ПОСЛЕ сохранения (строка 193)
**Вердикт:** ПОДТВЕРЖДЕНА
**Это корневая причина.**

### H2 — Backend передаёт весь `dto.Grades` в уведомления
**Файл:** `backend/Api/Controllers/CoursesController.cs`, строка 308
**Факты:**
- `NotifyGradesSavedSafeAsync(id, dto.Grades)` — передаётся весь список
- Backend не делает diff с существующими записями в БД
**Вердикт:** ПОДТВЕРЖДЕНА (следствие H1)

### H3 — `BuildEmailBody` рендерит все переданные записи
**Файл:** `backend/Api/Services/GradeNotificationAdapter.cs`, строка 142
**Факты:**
- `foreach (var grade in grades)` — итерация по всем переданным записям
- Нет фильтрации по isNew/isChanged
**Вердикт:** ПОДТВЕРЖДЕНА (следствие H1)

### H4 — `hasChanges` не используется для фильтрации payload
**Файл:** `frontend/src/views/CourseProgress.vue`, строки 147-149, 47
**Факты:**
- `markChanged()` сравнивает JSON `grades.value` vs `originalGrades.value`
- `hasChanges` используется только для `:disabled` кнопки Save
- В `saveGrades()` не используется для diff
**Вердикт:** ПОДТВЕРЖДЕНА (дополнительное подтверждение)

## Первопричина

**`frontend/src/views/CourseProgress.vue:saveGrades()` (строки 183-196)**

Функция `saveGrades()` собирает все непустые оценки из реактивного объекта `grades.value` и отправляет их на сервер. При этом не вычисляется разница (diff) между текущими значениями и исходными (`originalGrades.value`), зафиксированными при загрузке страницы. В результате при каждом сохранении сервер получает все заполненные оценки студента, а не только изменённые.

Серверная часть (`CoursesController.SaveGrades` → `NotifyGradesSavedSafeAsync` → `GradeNotificationAdapter.NotifyGradesSavedAsync`) корректно обрабатывает полученный payload, но поскольку payload содержит все оценки, уведомление по электронной почте также включает все оценки.

## Рекомендуемое исправление

Изменить `saveGrades()` в `CourseProgress.vue` так, чтобы она отправляла только записи, где `grades.value[key] !== originalGrades.value[key]`.

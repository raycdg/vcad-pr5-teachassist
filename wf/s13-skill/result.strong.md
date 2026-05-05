## Отчёт

**Проблема устранена.**

**Первопричина:** `frontend/src/views/CourseProgress.vue:saveGrades()` отправляла все непустые оценки, а не только изменённые. Функция `markChanged()` отслеживала изменения для состояния кнопки Save, но этот tracking не использовался при формировании payload.

**Изменения:**

| Файл | Изменение |
|------|-----------|
| `frontend/src/views/CourseProgress.vue` | В `saveGrades()` добавлен фильтр `v !== originalGrades.value[key]` — отправляются только изменённые записи. Добавлена guard-проверка `entries.length === 0` для пропуска пустого сохранения. |

**Тесты:** 126/126 backend тестов зелёные, ESLint без ошибок.

**Commit message:**
```
fix(frontend): send only changed grades in course progress save

The saveGrades function was sending all non-empty grades to the server
on every save, causing email notifications to include unchanged grades.
Now only grades that differ from originalGrades are sent.
```
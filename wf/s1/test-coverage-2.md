## Отчет: Актуальное покрытие тестами backend API (2026-04-30, повторный анализ)

---

### Сводка по контроллерам

| Контроллер | Маршрутов | Тестов | Покрытие веток |
|---|---|---|---|
| **CoursesController** | 11 | 28 | ~95% |
| **DisciplinesController** | 5 | 17 | ~95% |
| **GroupsController** | 5 | 14 | ~95% |
| **StudentsController** | 5 | 13 | ~90% |
| **TasksController** | 5 | 25 | ~95% |
| **ИТОГО** | **31 маршрут** | **97 тестов** | |

---

### Подробная матрица покрытия

#### CoursesController — 28 тестов

| Маршрут | Покрытие | Тесты |
|---|---|---|
| `GET /api/courses` | Полное | showAll=false, showAll=true |
| `GET /api/courses/{id}` | Полное | not found, exists |
| `POST /api/courses` | Полное | valid, discipline not found, group not found |
| `PUT /api/courses/{id}` | Полное | not found, discipline/group not found, success |
| `PATCH /api/courses/{id}/toggle-status` | Полное | flips isActive |
| `DELETE /api/courses/{id}` | Полное | not found, success |
| `GET /api/courses/{id}/progress` | Полное | not found, with students/tasks, with grades, empty |
| `POST /api/courses/{id}/grades` | Полное | inactive, not found, create, update, task not found, invalid binary, score exceeds, empty, valid binary, valid score |

#### DisciplinesController — 17 тестов

| Маршрут | Покрытие | Тесты |
|---|---|---|
| `GET /api/disciplines` | Полное | empty, ordered |
| `GET /api/disciplines/{id}` | Полное | not found, exists |
| `POST /api/disciplines` | Полное | valid, name exists, duplicate abbreviation, timestamps, race condition |
| `PUT /api/disciplines/{id}` | Полное | not found, success, name exists, same name allowed, timestamp, race condition |
| `DELETE /api/disciplines/{id}` | Полное | not found, success |

#### GroupsController — 14 тестов

| Маршрут | Покрытие | Тесты |
|---|---|---|
| `GET /api/groups` | Полное | empty, all, ordered |
| `GET /api/groups/{id}` | Полное | not found, exists |
| `POST /api/groups` | Полное | valid, name exists, race condition |
| `PUT /api/groups/{id}` | Полное | not found, success, name exists, race condition |
| `DELETE /api/groups/{id}` | Полное | not found, success |

#### StudentsController — 13 тестов

| Маршрут | Покрытие | Тесты |
|---|---|---|
| `GET /api/groups/{groupId}/students` | Полное | not found, empty, all, filtering, ordered |
| `GET /api/students/{id}` | Полное | not found, exists |
| `POST /api/students` | Полное | group not found, success |
| `PUT /api/students/{id}` | Полное | not found, success |
| `DELETE /api/students/{id}` | Полное | not found, success |

#### TasksController — 25 тестов

| Маршрут | Покрытие | Тесты |
|---|---|---|
| `GET /api/disciplines/{id}/tasks` | Полное | empty, ordered, search filter |
| `POST /api/disciplines/{id}/tasks` | Полное | binary, score, discipline not found, maxScore required, auto-increment, ignore maxScore for binary |
| `PUT /api/disciplines/{id}/tasks/{id}` | Полное | not found, wrong discipline, success, maxScore required, clears maxScore |
| `DELETE /api/disciplines/{id}/tasks/{id}` | Полное | not found, single delete, reorder, isolation |
| `PATCH /api/disciplines/{id}/tasks/{id}/priority` | Полное | not found, swap up, swap down, first up, last down, timestamps, isolation |

---

### Оставшиеся пробелы

| # | Контроллер | Тип пробела | Описание | Приоритет |
|---|---|---|---|---|
| 1 | **CoursesController** | Race condition | Нет тестов на конкурентное создание/обновление курса (есть в Groups/Disciplines) | Низкий |
| 2 | **StudentsController** | ModelState | Нет теста на валидацию DTO через атрибуты (Required, MaxLength) | Низкий |
| 3 | **CoursesController** | GetCourses ordering | Нет теста на сортировку (Year → DisciplineName) | Низкий |
| 4 | **StudentsController** | Race condition | Нет тестов на конкурентные операции | Низкий |
| 5 | **TasksController** | Race condition | Нет тестов на конкурентные операции | Низкий |
| 6 | **CoursesController** | SaveGrades — score=0 | Нет теста на граничное значение (score=0, valid) | Низкий |
| 7 | **CoursesController** | SaveGrades — negative score | Нет теста на отрицательное значение | Низкий |

---

### Итоговая оценка

- **Все основные бизнес-маршруты** покрыты (100%)
- **Критическая бизнес-логика** (валидация оценок, reordering задач, priority swap, уникальность имён) — покрыта
- **Edge cases** (race conditions, ModelState) — частично, низкий приоритет
- **Осталось ~7 нишевых сценариев**, все с низким приоритетом


## Тесты на race condition

### 1. GroupsController (2 теста)

| Тест | Метод | Сценарий | Ожидаемый результат |
|---|---|---|---|
| `CreateGroup_ReturnsBadRequest_WhenDuplicateNameConcurrent` | `POST /api/groups` | 2 параллельных запроса на создание группы с одинаковым именем "Concurrent Group" | Хотя бы один `BadRequest` (предотвращение дубликата) |
| `UpdateGroup_ReturnsBadRequest_WhenDuplicateNameConcurrent` | `PUT /api/groups/{id}` | 2 параллельных запроса на обновление группы id=1 с именем "Group B" (уже существует у группы id=2) | Хотя бы один `BadRequest` (предотвращение дубликата) |

### 2. DisciplinesController (2 теста)

| Тест | Метод | Сценарий | Ожидаемый результат |
|---|---|---|---|
| `CreateDiscipline_ReturnsBadRequest_WhenDuplicateNameConcurrent` | `POST /api/disciplines` | 2 параллельных запроса на создание дисциплины с одинаковым именем "Math" | Хотя бы один `BadRequest` (предотвращение дубликата) |
| `UpdateDiscipline_ReturnsBadRequest_WhenDuplicateNameConcurrent` | `PUT /api/disciplines/{id}` | 2 параллельных запроса на обновление дисциплины id=1 с именем "Discipline B" (уже существует у id=2) | Хотя бы один `BadRequest` (предотвращение дубликата) |

### Общий паттерн

Все 4 теста используют одинаковую схему:
1. Создают общий `DbContext` с существующими данными
2. Запускают 2 одинаковых запроса **параллельно** через `Task.WhenAll`
3. Проверяют, что хотя бы один запрос вернул `BadRequest`
4. Цель — гарантировать, что **невозможно создать дубликат имени** при конкурентном доступе

### Отсутствуют race condition тесты для

| Контроллер | Почему отсутствует |
|---|---|
| **CoursesController** | Создание/обновление курса проверяет существование Discipline/Group по ID, а не уникальность имени — race condition менее критичен |
| **StudentsController** | Нет проверки уникальности (имя + фамилия не уникальны) |
| **TasksController** | Нет проверки уникальности имени задачи |

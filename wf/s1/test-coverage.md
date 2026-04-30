# Отчет: Покрытие тестами backend API

> Дата: 2026-04-30
> Проект: TeachAssist

---

## Структура API (контроллеры и маршруты)

| Контроллер | Маршрут | Метод | Покрыт тестами? |
|---|---|---|---|
| **CoursesController** | `GET /api/courses` | GetCourses (showAll=false) | Да |
| | `GET /api/courses` | GetCourses (showAll=true) | Да |
| | `GET /api/courses/{id}` | GetCourse | **Нет** |
| | `POST /api/courses` | CreateCourse (valid) | Да |
| | `POST /api/courses` | CreateCourse (invalid discipline/group) | **Нет** |
| | `PUT /api/courses/{id}` | UpdateCourse | **Нет** |
| | `PATCH /api/courses/{id}/toggle-status` | ToggleStatus | Да |
| | `DELETE /api/courses/{id}` | DeleteCourse | **Нет** |
| | `GET /api/courses/{id}/progress` | GetProgress | **Нет** |
| | `POST /api/courses/{id}/grades` | SaveGrades (inactive) | Да |
| | `POST /api/courses/{id}/grades` | SaveGrades (valid, task not found, invalid grade) | **Нет** |
| **DisciplinesController** | `GET /api/disciplines` | GetDisciplines | **Нет** |
| | `GET /api/disciplines/{id}` | GetDiscipline | **Нет** |
| | `POST /api/disciplines` | CreateDiscipline | **Нет** |
| | `PUT /api/disciplines/{id}` | UpdateDiscipline | **Нет** |
| | `DELETE /api/disciplines/{id}` | DeleteDiscipline | **Нет** |
| **GroupsController** | `GET /api/groups` | GetGroups (empty, all, ordering) | Да |
| | `GET /api/groups/{id}` | GetGroup (not found, exists) | Да |
| | `POST /api/groups` | CreateGroup (valid, duplicate, race) | Да |
| | `PUT /api/groups/{id}` | UpdateGroup (not found, valid, duplicate, race) | Да |
| | `DELETE /api/groups/{id}` | DeleteGroup (not found, exists) | Да |
| **StudentsController** | `GET /api/groups/{groupId}/students` | GetStudentsByGroup (not found, empty, all, filtering, ordering) | Да |
| | `GET /api/students/{id}` | GetStudent (not found, exists) | Да |
| | `POST /api/students` | CreateStudent (not found group, valid) | Да |
| | `PUT /api/students/{id}` | UpdateStudent (not found, valid) | Да |
| | `DELETE /api/students/{id}` | DeleteStudent (not found, exists) | Да |
| **TasksController** | `GET /api/disciplines/{id}/tasks` | GetTasks (with/without search) | **Нет** |
| | `POST /api/disciplines/{id}/tasks` | CreateTask | **Нет** |
| | `PUT /api/disciplines/{id}/tasks/{id}` | UpdateTask | **Нет** |
| | `DELETE /api/disciplines/{id}/tasks/{id}` | DeleteTask (+ reordering) | **Нет** |
| | `PATCH /api/disciplines/{id}/tasks/{id}/priority` | ChangePriority (up, down, invalid) | **Нет** |

---

## Значимые uncovered маршруты и логика (приоритет由高到低)

### 1. **TasksController** — полностью не покрыт (5 маршрутов)

- `GetTasks` с фильтрацией по search
- `CreateTask` с валидацией MaxScore, авто-нумерацией
- `UpdateTask` с валидацией MaxScore
- `DeleteTask` с автоматическим reordering номеров
- `ChangePriority` (up/down) с swap номеров

### 2. **DisciplinesController** — полностью не покрыт (5 маршрутов)

- `GetDisciplines` / `GetDiscipline`
- `CreateDiscipline` с проверкой уникальности имени
- `UpdateDiscipline` с проверкой уникальности имени
- `DeleteDiscipline`

### 3. **CoursesController** — частично не покрыт

- `GetCourse` (GET by id, not found case)
- `UpdateCourse` (PUT) — валидация discipline/group, update полей
- `DeleteCourse` — удаление курса
- `GetProgress` — сложная логика сборки прогресса (студенты + задачи + оценки)
- `SaveGrades` — позитивный кейс, task not found, валидация grade (binary/score grading)
- `CreateCourse` — invalid discipline/group id

### 4. **Общие пробелы в покрытых контроллерах**

- **CoursesController**: отсутствует тест на race condition при создании/обновлении (есть в Groups)
- **StudentsController**: нет теста на валидацию ModelState (invalid DTO)

---

## Итоговая статистика

| Метрика | Значение |
|---|---|
| Всего маршрутов | 25 |
| Покрыто тестами | 14 |
| Не покрыто | 11 |
| Контроллер без тестов | 2 из 5 (Disciplines, Tasks) |

## Рекомендуемые приоритеты для написания тестов

1. **TasksController** — самая сложная логика (reordering, priority swap, grading types)
2. **CoursesController.SaveGrades** — критичная бизнес-логика с валидацией оценок
3. **CoursesController.GetProgress** — агрегация данных из 3 таблиц
4. **DisciplinesController** — CRUD с валидацией уникальности
5. **CoursesController.UpdateCourse / DeleteCourse / GetCourse** — стандартные CRUD

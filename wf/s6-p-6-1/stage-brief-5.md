# Stage Brief: Этап 5 — Авторизация на уровне ресурсов

## Цель
Реализовать ограничение доступа преподавателя только к собственным ресурсам (предметы, задания, курсы, оценки) с поддержкой нескольких преподавателей на одну дисциплину/курс. Преподаватель видит все группы и дисциплины, может создавать курсы на любую дисциплину. Менеджер управляет назначениями преподавателей.

## Точки встраивания

### Backend — модели и БД
- `backend/Models/` — добавить связующие сущности `DisciplineTeacher` и `CourseTeacher` для связи многие-ко-многим `AppUser ↔ Discipline` и `AppUser ↔ Course`
- `backend/Data/AppDbContext.cs` — добавить DbSet и настроить связи через Fluent API
- `backend/migrations/` — создать новую миграцию для таблиц `DisciplineTeachers` и `CourseTeachers`

### Backend — авторизация
- `backend/Authorization/` — создать `ResourceOwnerAuthorizationHandler` для проверки принадлежности ресурса пользователю
- `backend/Program.cs` — зарегистрировать handler в DI

### Backend — контроллеры
- `backend/api/DisciplinesController.cs` — обновить GET (преподаватель видит все), POST (преподаватель создаёт), PUT/DELETE (только свои дисциплины)
- `backend/api/TasksController.cs` — преподаватель редактирует только задачи своих дисциплин
- `backend/api/CoursesController.cs` — обновить GET (только свои курсы), POST (на любую дисциплину), PUT/DELETE/PATCH (только свои), оценки (только свои курсы)
- `backend/api/DisciplinesController.cs` + `backend/api/CoursesController.cs` — добавить endpoints назначения/снятия преподавателей (Manager+): `POST /api/disciplines/{id}/assign-teacher`, `POST /api/courses/{id}/assign-teacher`, `DELETE /api/disciplines/{id}/teachers/{teacherId}`, `DELETE /api/courses/{id}/teachers/{teacherId}`

### Tests
- `tests/` — написать тесты на `ResourceOwnerAuthorizationHandler` и обновлённые контроллеры

## Инварианты
- Не изменять инфраструктуру JWT и аутентификации (этап 1)
- Не изменять логику управления пользователями (этап 2)
- Не изменять политики ролей `RequireTeacher`, `RequireManager`, `RequireAdmin` (этап 3)
- Не изменять личный кабинет `/api/account/*` (этап 4)
- Не изменять frontend (этап 6), кроме случаев, если потребуется минимальная адаптация API-вызовов
- Не менять существующие миграции, создавать только новые
- Не менять структуру моделей `Group`, `Student`, `AppUser` (кроме добавления связей)

## Критерии готовности
- На одну дисциплину/курс можно назначить нескольких преподавателей
- Преподаватель GET `/api/disciplines` возвращает все дисциплины
- Преподаватель НЕ может PUT/DELETE дисциплину, где он не назначен (403)
- Преподаватель может POST `/api/courses` с `DisciplineId` любой дисциплины
- Преподаватель при создании курса видит список всех дисциплин
- Преподаватель GET `/api/courses` возвращает только курсы, где он назначен
- Менеджер может назначить/снять преподавателя с дисциплины/курса через API
- Менеджер и администратор имеют полный доступ ко всем ресурсам
- Написаны и проходят тесты на authorization handlers
- Все существующие тесты backend продолжают проходить

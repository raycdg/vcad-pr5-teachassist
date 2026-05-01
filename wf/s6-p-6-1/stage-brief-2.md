# Stage Brief: Этап 2 — Управление пользователями (администратор)

## Цель

Реализовать полный CRUD управления пользователями системы, доступный только администратору. Включает создание, просмотр, редактирование ролей, сброс пароля, мягкое удаление и восстановление пользователей. Frontend — страница `/admin/users` с таблицей и формами.

---

## Точки встраивания

### Backend

| Файл / Путь | Действие |
|---|---|
| `backend/Program.cs` | Seed роли `Admin` и назначение её админу по умолчанию; добавить политику `RequireAdmin` (`RequireRole("Admin")`); добавить claim `role` в JWT через `GenerateJwtToken` |
| `backend/Api/Controllers/AuthController.cs` | В `GenerateJwtToken` добавить claim `role` — получить роли пользователя через `GetRolesAsync` и добавить `Claim(ClaimTypes.Role, role)` |
| `backend/Api/DTOs/AuthDtos.cs` | Добавить `LoginResponse.Role` (string) |
| `backend/Api/DTOs/UserDtos.cs` | **Новый файл** — DTO: `UserDto` (id, email, role, isDeleted, createdAt), `CreateUserDto` (email, password, role), `UpdateUserRoleDto` (role), `ResetPasswordDto` (newPassword) |
| `backend/Api/Controllers/UsersController.cs` | **Новый файл** — 7 endpoints (все с `[Authorize(Policy = "RequireAdmin")]`): GET `/api/users`, GET `/api/users/{id}`, POST `/api/users`, PUT `/api/users/{id}/role`, PUT `/api/users/{id}/reset-password`, DELETE `/api/users/{id}`, POST `/api/users/{id}/restore` |

### Frontend

| Файл / Путь | Действие |
|---|---|
| `frontend/src/stores/auth.js` | Добавить поле `role` в state, обновить `login()` для сохранения роли, добавить getter `isAdmin` |
| `frontend/src/views/admin/Users.vue` | **Новый файл** — страница `/admin/users`: таблица пользователей (email, role, статус), формы создания/редактирования роли, кнопки удаления/восстановления, сброс пароля |
| `frontend/src/router/index.js` | Добавить роут `/admin/users` с `meta: { requiresAdmin: true }`, обновить `beforeEach` guard для проверки роли |
| `frontend/src/App.vue` | Добавить ссылку «Users» в навбар (только для Admin), обновить отображение роли в шапке |

### Тесты

| Файл / Путь | Действие |
|---|---|
| `tests/TeachAssist.Api.Tests/UsersControllerTests.cs` | **Новый файл** — тесты на UsersController: успешные CRUD-операции, 403 для не-Admin, soft delete, restore |

### Миграции

| Файл / Путь | Действие |
|---|---|
| `backend/Migrations/Auth/` | **Новая миграция** — добавление `normalized_email` фильтра для soft-delete совместимости (если нужно), seed роли `Admin` через код (не миграция) |

---

## Инварианты (НЕ трогать)

- **`backend/domain/Data/AppDbContext.cs`** — не менять
- **`backend/domain/Models/`** — не менять существующие модели
- **Существующие контроллеры** (`GroupsController`, `StudentsController`, `DisciplinesController`, `TasksController`, `CoursesController`) — не менять логику, не добавлять `[Authorize]` (это будет этап 3)
- **Существующие тесты** — все ~104 тестов должны проходить без изменений
- **Существующие миграции** — не менять
- **`frontend/src/views/`** — существующие 7 Vue-представлений не менять (кроме `App.vue` и `Login.vue` — только добавить role в response handling)
- **`frontend/src/stores/disciplines.js` и другие stores** — не менять
- **`frontend/src/main.js`** — не менять axios interceptors

---

## Критерии готовности

### Backend
1. Роль `Admin` создана и назначена пользователю `admin@teachassis.local`
2. JWT содержит claim `role` со значением роли пользователя
3. `GET /api/users` возвращает список пользователей без удалённых
4. `GET /api/users?includeDeleted=true` возвращает всех, включая удалённых
5. `POST /api/users` создаёт пользователя с email, паролем и ролью
6. `PUT /api/users/{id}/role` меняет роль пользователя
7. `PUT /api/users/{id}/reset-password` сбрасывает пароль
8. `DELETE /api/users/{id}` — мягкое удаление (IsDeleted = true), удалённый не может войти
9. `POST /api/users/{id}/restore` — восстановление удалённого пользователя
10. Не-Admin пользователь получает 403 на всех endpoints UsersController
11. Созданный пользователь может войти с заданным паролем

### Frontend
12. Страница `/admin/users` доступна только Admin (не-Admin → редирект на `/` или 403)
13. Таблица показывает email, роль, статус (активен/удалён)
14. Форма создания пользователя: email, пароль, выбор роли (Teacher, Manager, Admin)
15. Кнопка удаления пользователя (мягкое удаление)
16. Кнопка восстановления удалённого пользователя
17. Функция сброса пароля
18. Ссылка «Users» в навбаре видна только Admin
19. Роль пользователя отображается в шапке рядом с email

### Тесты
20. Все существующие ~104 тестов проходят
21. Написаны и проходят новые тесты `UsersControllerTests` (минимум 6)
22. Написаны и проходят тесты на JWT с role claim

---

## Сигналы остановки

- **Soft delete ломает уникальность email**: удалённый пользователь с `admin@test.local` мешает создать нового с тем же email — нужно обсудить стратегию (suffix, очистка normalized_email)
- **Требуется импорт из файла (CSV/Excel)**: выходит за рамки этапа
- **Обнаружена необходимость в логировании действий администратора**: обсудить отдельно
- **Тесты AuthController ломались**: после добавления role claim в JWT нужно обновить тесты авторизации

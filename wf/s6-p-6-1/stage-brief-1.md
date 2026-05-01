# Stage Brief: Этап 1 — Инфраструктура аутентификации (JWT)

## Цель

Добавить в проект TeachAssist базовую JWT-аутентификацию: модель пользователя с soft delete, вход по email/паролю, seed учётки администратора по умолчанию, защищённый frontend с логином. Регистрация пользователей не реализуется — всех создаёт администратор (этап 2).

---

## Точки встраивания

### Backend

| Файл / Путь | Действие |
|---|---|
| `backend/TeachAssist.Api.csproj` | Добавить NuGet-пакеты: `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore` |
| `backend/domain/Models/AppUser.cs` | **Новый файл** — модель `AppUser : IdentityUser`, поля: `IsDeleted` (bool), `CreatedAt` (DateTime), `UpdatedAt` (DateTime) |
| `backend/domain/Data/AppDbContext.cs` | **Создать новый DbContext** `AuthDbContext : IdentityDbContext<AppUser>` (не смешивать с `DomainDbContext`). Настроить таблицу `users` с lowercase column names, глобальный фильтр `HasQueryFilter(u => !u.IsDeleted)` |
| `backend/appsettings.json` | Добавить секцию `Jwt` с `Issuer`, `Audience`, `SecretKey` (секрет, валидный 30+ символов) |
| `backend/Program.cs` | Добавить регистрацию Identity (`AddIdentity<AppUser, IdentityRole>`), JWT-аутентификацию (`AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)`), `UseAuthentication()`, `UseAuthorization()` до `MapControllers()`. Seed админа при старте. |
| `backend/Api/Controllers/AuthController.cs` | **Новый файл** — `POST /api/auth/login` (принимает `{email, password}`, возвращает `{token, email}`) |
| `backend/Migrations/` | **Новая миграция** — таблицы Identity (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserTokens`, `AspNetRoleClaims`, `AspNetUserClaims`, `AspNetUserLogins`). Колонка `IsDeleted` в `AspNetUsers`. Seed админа. |

### Frontend

| Файл / Путь | Действие |
|---|---|
| `frontend/src/views/Login.vue` | **Новый файл** — страница логина с формой email/password, валидация, отправка POST `/api/auth/login`, сохранение JWT в `localStorage` |
| `frontend/src/router/index.js` | Добавить роут `/login`, `beforeEach` guard: если нет токена и роут не `/login` → редирект на `/login` |
| `frontend/src/main.js` | Создать axios instance с базовым URL, добавить interceptor для подстановки `Authorization: Bearer <token>` из localStorage |
| `frontend/src/stores/auth.js` | **Новый файл** (Pinia) — состояние auth: `token`, `email`; actions: `login()`, `logout()`, `isLoggedIn()` |
| `frontend/src/App.vue` | Добавить кнопку «Logout» в `v-app-bar`, показать email пользователя |

### Тесты

| Файл / Путь | Действие |
|---|---|
| `tests/TeachAssist.Api.Tests/AuthControllerTests.cs` | **Новый файл** — тесты на login: успешный вход, неверный пароль, несуществующий email, заблокированный (IsDeleted) пользователь |
| Существующие `*ControllerTests.cs` | Не меняются — на этом этапе `[Authorize]` не ставится на существующие контроллеры, API остаётся открытым |

---

## Инварианты (НЕ трогать)

- **`backend/domain/Data/AppDbContext.cs`** — не менять конфигурацию существующих моделей (Groups, Students, Disciplines, Tasks, Courses, StudentGrades)
- **`backend/domain/Models/`** — не менять существующие модели (DomainGroup, Student, Discipline, DisciplineTask, Course, StudentGrade)
- **Существующие контроллеры** (`GroupsController`, `StudentsController`, `DisciplinesController`, `TasksController`, `CoursesController`) — не менять логику, не добавлять `[Authorize]`
- **Существующие тесты** — все ~100 тестов должны проходить без изменений
- **Существующие миграции** в `backend/Migrations/` — не менять
- **`frontend/src/views/`** — существующие 7 Vue-представлений не менять (кроме `App.vue`)
- **`frontend/src/stores/`** — существующие 5 stores не менять

---

## Критерии готовности

### Backend
1. `POST /api/auth/login` с `admin@teachassis.local` / `admin` возвращает 200 и JWT-токен
2. `POST /api/auth/login` с неверным паролем возвращает 401
3. `POST /api/auth/login` с несуществующим email возвращает 401
4. JWT валидируется — подпись, issuer, audience, срок жизни
5. Seed админа работает при первом запуске (idempotent — не дублирует)
6. `IsDeleted` глобальный фильтр настроен — удалённые пользователи не видны и не могут войти
7. Миграция Identity создана и применяется к БД

### Frontend
8. Страница `/login` отображается без авторизации
9. При входе с корректными данными — редирект на `/`, JWT сохранён в localStorage
10. При входе с неверными данными — показывается ошибка
11. Без токена прямой переход на `/groups`, `/disciplines`, `/courses` → редирект на `/login`
12. Кнопка Logout в шапке, очищает localStorage, редирект на `/login`
13. Email пользователя отображается в шапке после входа

### Тесты
14. Все существующие ~100 тестов проходят
15. Написаны и проходят новые тесты `AuthControllerTests` (минимум 4)

---

## Сигналы остановки

- **Конфликт DbContext**: Identity требует свой DbContext, а проект использует `DomainDbContext`. Если невозможно разделить без поломки существующей логики — остановиться и обсудить архитектуру
- **Проблемы с CORS**: если frontend не может отправить авторизационный заголовок на backend
- **Требуются refresh-токены**: выходит за рамки этапа, обсудить отдельно
- **Seed админа не работает**: если UserManager не может создать пользователя из-за password policy — не ослаблять policy молча, а сообщить

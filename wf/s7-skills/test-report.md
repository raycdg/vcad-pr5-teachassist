## Отчет о покрытии тестами backend

**Общие показатели:**
- Строка: **24.37%** (1283 из 5263)
- Ветвления: **40.27%** (236 из 586)
- Пройдено тестов: **126**

---

### Покрытые файлы (Strong: >90%)

| Файл | Покрытие строк | Покрытие ветвлений |
|-------|------------------|---------------------|
| Domain/Models/* (6 моделей) | 90-100% | 100% |
| Domain/Data/AppDbContext | 100% | 100% |
| Api/Controllers/AccountController | 100% | 100% |
| Api/Controllers/AuthController | 100% | 100% |
| Api/Controllers/CoursesController | 97% | 78.6% |
| Api/Controllers/DisciplinesController | 100% | 100% |
| Api/Controllers/GroupsController | 100% | 100% |
| Api/Controllers/StudentsController | 100% | 100% |
| Api/Controllers/TasksController | 100% | 100% |
| Api/Services/GradeNotificationAdapter | 100% (строки) | 60% (ветвления) |

---

### Частично покрытые (Medium: 50-89%)

| Файл/Метод | Покрытие |
|--------------|----------|
| UsersController/CreateUser | 70% |
| UsersController/ResetPassword | 50% |
| UsersController/RestoreUser | 76% |
| UsersController/UpdateUserRole | 82.6% |
| UsersController/GetUser | **0%** ⚠️ |

---

### Не покрытые (Weak/None: 0%) — приоритет исправления

**Первый приоритет (логика контроллеров):**
- `Api/Authorization/ResourceOwnerAuthorizationHandler` — 0%
- `Api/Controllers/CoursesController/AssignTeacher` — 0%
- `Api/Controllers/CoursesController/RemoveTeacher` — 0%
- `Api/Controllers/DisciplinesController/AssignTeacher` — 0%
- `Api/Controllers/DisciplinesController/RemoveTeacher` — 0%
- `Api/Controllers/UsersController/GetUser` — 0%

**Второй приоритет (инфраструктура):**
- `Api/Logging/FileLoggerProvider` — 0%
- `Program.cs` (запуск приложения) — 0%

**Не требует покрытия:**
- `Migrations/*` — 0% (сгенерированный код)
- `DomainDbContextFactory` — 0% (factory для миграций)

---

### Рекомендации

1. **Добавить тесты на авторизацию** — `ResourceOwnerAuthorizationHandler` полностью не покрыт
2. **Покрыть методы AssignTeacher/RemoveTeacher** в CoursesController и DisciplinesController
3. **Добавить тест на GetUser** в UsersController (сейчас 0%)
4. **Улучшить покрытие ветвлений** в CoursesController (78.6%) и UsersController (50-82%)
5. **Протестировать SMS/Email уведомления** — в GradeNotificationAdapter низкое покрытие ветвлений (60%)
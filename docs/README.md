# TeachAssist - debugging, tests, refactoring

## Описание

TeachAssist — веб-приложение для помощи преподавателю университета в управлении расписанием занятий, контроле посещаемости студентов, выполнении заданий и аттестации.

## Стек технологий

- **Frontend**: VueJS 3, Vuetify 3, Vite, Pinia, Vue Router
- **Backend**: ASP.NET Core 10, Entity Framework Core
- **Database**: PostgreSQL
- **Testing**: xUnit, Microsoft.EntityFrameworkCore.InMemory

## Архитектура проекта

```
teachassist-4/
├── backend/
│   ├── TeachAssist.Api.csproj      # Основной проект API
│   ├── Program.cs                   # Точка входа
│   ├── appsettings.json             # Конфигурация
│   ├── domain/
│   │   ├── TeachAssist.Domain.csproj
│   │   ├── Models/
│   │   │   ├── Group.cs           # Модель DomainGroup
│   │   │   └── Student.cs        # Модель Student
│   │   └── Data/AppDbContext.cs   # DomainDbContext
│   ├── Api/
│   │   ├── Controllers/
│   │   │   ├── GroupsController.cs   # CRUD для групп
│   │   │   └── StudentsController.cs # CRUD для студентов
│   │   └── DTOs/
│   │       ├── GroupDtos.cs       # DTOs для групп
│   │       └── StudentDtos.cs     # DTOs для студентов
│   ├── Migrations/                  # EF Core миграции
│   └── Api.Tests/                   # Unit тесты
├── frontend/
│   ├── vite.config.js               # Конфигурация Vite
│   ├── package.json
│   └── src/
│       ├── main.js                  # Инициализация Vue
│       ├── App.vue                  # Корневой компонент
│       ├── router/index.js          # Роутер
│       ├── stores/
│       │   ├── groups.js          # Pinia store для групп
│       │   └── students.js        # Pinia store для студентов
│       └── views/
│           ├── Dashboard.vue       # Заглавная страница
│           ├── Groups.vue          # CRUD страница групп
│           └── GroupDetail.vue     # Просмотр студентов группы
└── docs/
    └── README.md
```

## Реализованный функционал (Срез 1-2)

### Backend API

#### Группы студентов (CRUD)

| Метод | Endpoint | Описание |
|-------|----------|----------|
| GET | `/api/groups` | Получить все группы |
| GET | `/api/groups/{id}` | Получить группу по ID |
| POST | `/api/groups` | Создать новую группу |
| PUT | `/api/groups/{id}` | Обновить группу |
| DELETE | `/api/groups/{id}` | Удалить группу |

#### Студенты (CRUD)

| Метод | Endpoint | Описание |
|-------|----------|----------|
| GET | `/api/groups/{groupId}/students` | Получить студентов группы |
| GET | `/api/students/{id}` | Получить студента по ID |
| POST | `/api/students` | Создать студента |
| PUT | `/api/students/{id}` | Обновить студента |
| DELETE | `/api/students/{id}` | Удалить студента |

#### Модель Group

```csharp
public class DomainGroup
{
    public int Id { get; set; }
    public string Name { get; set; }      // Название группы
    public string ShortName { get; set; } // Сокращение
    public int YearStarted { get; set; }  // Год поступления
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<Student> Students { get; set; }
}
```

#### Модель Student

```csharp
public class Student
{
    public int Id { get; set; }
    public string FirstName { get; set; }  // Имя (обязательное)
    public string LastName { get; set; }   // Фамилия (обязательное)
    public string? Email { get; set; }    // Email (опциональный)
    public int GroupId { get; set; }      // FK к группе
    public DomainGroup Group { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### Frontend

#### Страница Dashboard
- Заглавная страница с приветствием
- Карточки быстрого доступа к разделам
- Навигация между страницами

#### Страница Groups (CRUD)
- Таблица со списком групп
- Добавление новой группы
- Редактирование существующей группы
- Удаление группы
- Форма с валидацией
- Кнопка "View Students" для перехода к студентам группы

#### Страница GroupDetail
- Информация о группе (название, сокращение, год)
- Таблица студентов группы
- Добавление нового студента
- Редактирование студента
- Удаление студента
- Форма с валидацией
- Навигация "Back to Groups"

## Запуск

### Backend

```bash
cd backend
dotnet run
# API доступно на http://localhost:5000
```

### Frontend

```bash
cd frontend
npm install
npm run dev
# Frontend доступно на http://localhost:5173
```

Vite настроен с проксированием `/api` запросов на `http://localhost:5000`.

## База данных

- **Host**: localhost
- **Port**: 5433
- **Database**: teachassist_3_1
- **Username**: postgres
- **Password**: postgres

### Миграции

```bash
cd backend
dotnet ef migrations add InitialCreate
dotnet ef migrations add AddStudents
dotnet ef database update
```

## Тесты

```bash
cd backend
dotnet test
# 23 теста (10 для GroupsController + 13 для StudentsController)
```

### Тесты GroupsController

1. `GetGroups_ReturnsEmptyList_WhenNoGroupsExist` — пустой список при отсутствии групп
2. `GetGroups_ReturnsAllGroups` — возвращает все группы
3. `GetGroup_ReturnsNotFound_WhenGroupDoesNotExist` — 404 для несуществующей группы
4. `GetGroup_ReturnsGroup_WhenExists` — возврат группы по ID
5. `CreateGroup_ReturnsCreatedGroup` — создание группы
6. `UpdateGroup_ReturnsNotFound_WhenGroupDoesNotExist` — 404 при обновлении несуществующей
7. `UpdateGroup_ReturnsUpdatedGroup_WhenGroupExists` — обновление группы
8. `DeleteGroup_ReturnsNotFound_WhenGroupDoesNotExist` — 404 при удалении несуществующей
9. `DeleteGroup_ReturnsNoContent_WhenGroupExists` — удаление группы
10. `GetGroups_OrdersByYearStartedThenName` — сортировка по году и названию

### Тесты StudentsController

1. `GetStudentsByGroup_ReturnsNotFound_WhenGroupDoesNotExist` — 404 для несуществующей группы
2. `GetStudentsByGroup_ReturnsEmptyList_WhenNoStudentsInGroup` — пустой список
3. `GetStudentsByGroup_ReturnsAllStudents_WhenStudentsExist` — возвращает всех студентов
4. `GetStudentsByGroup_ReturnsOnlyStudentsFromGroup` — только студенты указанной группы
5. `GetStudent_ReturnsNotFound_WhenStudentDoesNotExist` — 404 для несуществующего студента
6. `GetStudent_ReturnsStudent_WhenExists` — возврат студента по ID
7. `CreateStudent_ReturnsBadRequest_WhenGroupDoesNotExist` — ошибка при несуществующей группе
8. `CreateStudent_ReturnsCreatedStudent` — создание студента
9. `UpdateStudent_ReturnsNotFound_WhenStudentDoesNotExist` — 404 при обновлении несуществующего
10. `UpdateStudent_ReturnsUpdatedStudent` — обновление студента
11. `DeleteStudent_ReturnsNotFound_WhenStudentDoesNotExist` — 404 при удалении несуществующего
12. `DeleteStudent_ReturnsNoContent_WhenStudentExists` — удаление студента
13. `GetStudentsByGroup_OrdersByLastNameThenFirstName` — сортировка по фамилии и имени

## Следующие шаги (Sprint 2)

- [ ] Расписание занятий
- [ ] Журнал посещаемости
- [ ] Управление заданиями
- [ ] Аттестация студентов
- [ ] Надбавки за активность

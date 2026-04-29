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
teachassist-5/
├── backend/
│   ├── TeachAssist.Api.csproj      # Основной проект API
│   ├── Program.cs                   # Точка входа
│   ├── appsettings.json             # Конфигурация
│   ├── domain/
│   │   ├── TeachAssist.Domain.csproj
│   │   ├── Models/
│   │   │   ├── Group.cs           # Модель DomainGroup
│   │   │   ├── Student.cs        # Модель Student
│   │   │   ├── Discipline.cs     # Модель Discipline
│   │   │   ├── DisciplineTask.cs # Модель DisciplineTask
│   │   │   ├── Course.cs         # Модель Course (Итерация 2)
│   │   │   └── StudentGrade.cs   # Модель StudentGrade (Итерация 2)
│   │   └── Data/AppDbContext.cs   # DomainDbContext
│   ├── Api/
│   │   ├── Controllers/
│   │   │   ├── GroupsController.cs      # CRUD для групп
│   │   │   ├── StudentsController.cs    # CRUD для студентов
│   │   │   ├── DisciplinesController.cs # CRUD для предметов
│   │   │   ├── TasksController.cs       # CRUD для заданий
│   │   │   └── CoursesController.cs     # CRUD для курсов (Итерация 2)
│   │   └── DTOs/
│   │       ├── GroupDtos.cs          # DTOs для групп
│   │       ├── StudentDtos.cs        # DTOs для студентов
│   │       ├── DisciplineDtos.cs     # DTOs для предметов
│   │       ├── TaskDtos.cs           # DTOs для заданий
│   │       └── CourseDtos.cs         # DTOs для курсов (Итерация 2)
│   ├── Migrations/                  # EF Core миграции
│   └── Api.Tests/                   # Unit тесты
├── frontend/
│   ├── vite.config.js               # Конфигурация Vite
│   ├── package.json
│   └── src/
│       ├── main.js                  # Инициализация Vue
│       ├── App.vue                  # Корневой компонент (добавлен пункт Courses)
│       ├── router/index.js          # Роутер (добавлены /courses, /courses/:id/progress)
│       ├── stores/
│       │   ├── groups.js            # Pinia store для групп
│       │   ├── students.js         # Pinia store для студентов
│       │   ├── disciplines.js      # Pinia store для предметов
│       │   ├── tasks.js            # Pinia store для заданий
│       │   └── courses.js          # Pinia store для курсов (Итерация 2)
│       └── views/
│           ├── Dashboard.vue       # Заглавная страница
│           ├── Groups.vue          # CRUD страница групп
│           ├── GroupDetail.vue     # Просмотр студентов группы
│           ├── Disciplines.vue     # CRUD страница предметов
│           ├── Tasks.vue           # CRUD страница заданий
│           ├── Courses.vue         # CRUD страница курсов (Итерация 2)
│           └── CourseProgress.vue  # Страница прогресса (Итерация 2)
└── docs/
    └── README.md
```

## Реализованный функционал (Итерация 1-2)

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

#### Учебные предметы (CRUD - Итерация 1)

| Метод | Endpoint | Описание |
|-------|----------|----------|
| GET | `/api/disciplines` | Получить все предметы |
| GET | `/api/disciplines/{id}` | Получить предмет по ID |
| POST | `/api/disciplines` | Создать предмет |
| PUT | `/api/disciplines/{id}` | Обновить предмет |
| DELETE | `/api/disciplines/{id}` | Удалить предмет |

#### Задания (CRUD - Итерация 1)

| Метод | Endpoint | Описание |
|-------|----------|----------|
| GET | `/api/disciplines/{disciplineId}/tasks` | Получить задания предмета |
| GET | `/api/tasks/{id}` | Получить задание по ID |
| POST | `/api/tasks` | Создать задание |
| PUT | `/api/tasks/{id}` | Обновить задание |
| DELETE | `/api/tasks/{id}` | Удалить задание |
| PATCH | `/api/tasks/{id}/move` | Изменить порядковый номер |

#### Учебные курсы (CRUD + прогресс - Итерация 2)

| Метод | Endpoint | Описание |
|-------|----------|----------|
| GET | `/api/courses?showAll=false` | Получить активные курсы (showAll=true - все) |
| GET | `/api/courses/{id}` | Получить курс по ID |
| POST | `/api/courses` | Создать курс |
| PUT | `/api/courses/{id}` | Обновить курс |
| PATCH | `/api/courses/{id}/toggle-status` | Переключить статус активности |
| DELETE | `/api/courses/{id}` | Удалить курс |
| GET | `/api/courses/{id}/progress` | Получить данные прогресса (студенты, задания) |
| POST | `/api/courses/{id}/grades` | Пакетное сохранение оценок |

#### Модель Course (Итерация 2)

```csharp
public class Course
{
    public int Id { get; set; }
    public int DisciplineId { get; set; }
    public Discipline Discipline { get; set; }
    public int GroupId { get; set; }
    public DomainGroup Group { get; set; }
    public int Year { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

#### Модель StudentGrade (Итерация 2)

```csharp
public class StudentGrade
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; }
    public int DisciplineTaskId { get; set; }
    public DisciplineTask DisciplineTask { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; }
    public string? Value { get; set; }
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

#### Страница Disciplines (CRUD - Итерация 1)
- Таблица со списком предметов
- Добавление нового предмета
- Редактирование предмета
- Удаление предмета
- Поиск по названию

#### Страница Tasks (CRUD - Итерация 1)
- Таблица заданий предмета
- Добавление задания с выбором типа (Зачет/Балльная)
- Изменение порядка заданий
- Редактирование и удаление заданий

#### Страница Courses (CRUD - Итерация 2)
- Таблица курсов (по умолчанию только активные)
- Переключатель "Show all courses" / "Hide inactive"
- Создание курса (выбор предмета, группы, год)
- Редактирование курса, переключение статуса активности
- Кнопка перехода к прогрессу студентов
- Удаление курса

#### Страница CourseProgress (Итерация 2)
- Матрица: студенты (строки) × задания (столбцы)
- Фиксированная колонка с именами студентов при скролле
- Фильтрация студентов по имени и фамилии
- Сортировка по фамилии и имени
- Редактируемые ячейки для ввода оценок
- Кнопка "Save" для пакетного сохранения изменений
- Блокировка редактирования для неактивных курсов

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
- **Database**: teachassist
- **Username**: postgres
- **Password**: postgres

### Миграции

```bash
cd backend
dotnet ef database update
```

## Тесты

```bash
cd backend
dotnet test
# 27 тестов (10 для GroupsController + 13 для StudentsController + 4 для CoursesController)
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

### Тесты CoursesController (Итерация 2)

1. `GetCourses_ReturnsOnlyActive_ByDefault` — по умолчанию только активные
2. `GetCourses_ShowAll_ReturnsAll` — возврат всех при showAll=true
3. `CreateCourse_ValidDto_ReturnsCreated` — создание курса
4. `ToggleStatus_FlipsIsActive` — переключение статуса
5. `SaveGrades_InactiveCourse_ReturnsBadRequest` — блокировка сохранения для неактивного курса

## Следующие шаги (Итерация 3)

- [ ] Отправка email-уведомлений студентам о получении оценки
- [ ] Скрипт загрузки студентов из Excel (Итерация 4)
- [ ] Расписание занятий
- [ ] Журнал посещаемости
- [ ] Авторизация преподавателя и студентов

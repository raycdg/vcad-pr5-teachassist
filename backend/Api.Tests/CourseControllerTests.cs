using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TeachAssist.Api.Controllers;
using TeachAssist.Api.DTOs;
using TeachAssist.Api.Options;
using TeachAssist.Api.Services;
using TeachAssist.Domain.Data;
using TeachAssist.Domain.Models;

namespace TeachAssist.Api.Tests;

public class CourseControllerTests
{
    private DomainDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<DomainDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DomainDbContext(options);
    }

    private static GradeNotificationAdapter CreateStubAdapter()
    {
        var services = new ServiceCollection();
        services.AddDbContext<DomainDbContext>(options => options.UseInMemoryDatabase("stub"));
        services.AddLogging();
        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var logger = provider.GetRequiredService<ILogger<GradeNotificationAdapter>>();
        return new GradeNotificationAdapter(new SmtpOptions(), logger, scopeFactory);
    }

    [Fact]
    public async Task GetCourses_ReturnsOnlyActive_ByDefault()
    {
        using var context = GetDbContext();
        var discipline = new Discipline { Name = "D", Abbreviation = "D" };
        var group = new DomainGroup { Name = "G", ShortName = "G", YearStarted = 2024 };
        context.Disciplines.Add(discipline);
        context.Groups.Add(group);
        context.Courses.Add(new Course { Discipline = discipline, Group = group, Year = 2024, IsActive = true });
        context.Courses.Add(new Course { Discipline = discipline, Group = group, Year = 2024, IsActive = false });
        await context.SaveChangesAsync();

        var controller = new CoursesController(context, CreateStubAdapter());
        var actionResult = await controller.GetCourses();

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<CourseDto>>(ok.Value);
        Assert.Single(list);
    }

    [Fact]
    public async Task GetCourses_ShowAll_ReturnsAll()
    {
        using var context = GetDbContext();
        var discipline = new Discipline { Name = "D", Abbreviation = "D" };
        var group = new DomainGroup { Name = "G", ShortName = "G", YearStarted = 2024 };
        context.Disciplines.Add(discipline);
        context.Groups.Add(group);
        context.Courses.Add(new Course { Discipline = discipline, Group = group, Year = 2024, IsActive = true });
        context.Courses.Add(new Course { Discipline = discipline, Group = group, Year = 2024, IsActive = false });
        await context.SaveChangesAsync();

        var controller = new CoursesController(context, CreateStubAdapter());
        var actionResult = await controller.GetCourses(showAll: true);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<CourseDto>>(ok.Value);
        Assert.Equal(2, list.Count());
    }

    [Fact]
    public async Task CreateCourse_ValidDto_ReturnsCreated()
    {
        using var context = GetDbContext();
        var discipline = new Discipline { Name = "D", Abbreviation = "D" };
        var group = new DomainGroup { Name = "G", ShortName = "G", YearStarted = 2024 };
        context.Disciplines.Add(discipline);
        context.Groups.Add(group);
        await context.SaveChangesAsync();

        var controller = new CoursesController(context, CreateStubAdapter());
        var dto = new CreateCourseDto { DisciplineId = discipline.Id, GroupId = group.Id, Year = 2024 };

        var actionResult = await controller.CreateCourse(dto);

        var created = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
        var course = Assert.IsType<CourseDto>(created.Value);
        Assert.True(course.IsActive);
    }

    [Fact]
    public async Task ToggleStatus_FlipsIsActive()
    {
        using var context = GetDbContext();
        var discipline = new Discipline { Name = "D", Abbreviation = "D" };
        var group = new DomainGroup { Name = "G", ShortName = "G", YearStarted = 2024 };
        context.Disciplines.Add(discipline);
        context.Groups.Add(group);
        var course = new Course { Discipline = discipline, Group = group, Year = 2024, IsActive = true };
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var controller = new CoursesController(context, CreateStubAdapter());
        await controller.ToggleStatus(course.Id);

        var updated = await context.Courses.FindAsync(course.Id);
        Assert.False(updated!.IsActive);
    }

    [Fact]
    public async Task SaveGrades_InactiveCourse_ReturnsBadRequest()
    {
        using var context = GetDbContext();
        var discipline = new Discipline { Name = "D", Abbreviation = "D" };
        var group = new DomainGroup { Name = "G", ShortName = "G", YearStarted = 2024 };
        context.Disciplines.Add(discipline);
        context.Groups.Add(group);
        var course = new Course { Discipline = discipline, Group = group, Year = 2024, IsActive = false };
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var controller = new CoursesController(context, CreateStubAdapter());
        var dto = new BulkSaveGradesDto { Grades = new() };

        var result = await controller.SaveGrades(course.Id, dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetCourse_ReturnsNotFound_WhenCourseDoesNotExist()
    {
        using var context = GetDbContext();
        var controller = new CoursesController(context, CreateStubAdapter());

        var actionResult = await controller.GetCourse(999);

        Assert.IsType<NotFoundResult>(actionResult.Result);
    }

    [Fact]
    public async Task GetCourse_ReturnsCourse_WhenExists()
    {
        using var context = GetDbContext();
        var discipline = new Discipline { Name = "Math", Abbreviation = "MTH" };
        var group = new DomainGroup { Name = "Group A", ShortName = "GA", YearStarted = 2024 };
        context.Disciplines.Add(discipline);
        context.Groups.Add(group);
        var course = new Course { Discipline = discipline, Group = group, Year = 2024, IsActive = true };
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var controller = new CoursesController(context, CreateStubAdapter());
        var actionResult = await controller.GetCourse(course.Id);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var dto = Assert.IsType<CourseDto>(ok.Value);
        Assert.Equal("Math", dto.DisciplineName);
        Assert.Equal("Group A", dto.GroupName);
        Assert.Equal(2024, dto.Year);
        Assert.True(dto.IsActive);
    }

    [Fact]
    public async Task CreateCourse_ReturnsBadRequest_WhenDisciplineNotFound()
    {
        using var context = GetDbContext();
        var group = new DomainGroup { Name = "G", ShortName = "G", YearStarted = 2024 };
        context.Groups.Add(group);
        await context.SaveChangesAsync();

        var controller = new CoursesController(context, CreateStubAdapter());
        var dto = new CreateCourseDto { DisciplineId = 999, GroupId = group.Id, Year = 2024 };

        var actionResult = await controller.CreateCourse(dto);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task CreateCourse_ReturnsBadRequest_WhenGroupNotFound()
    {
        using var context = GetDbContext();
        var discipline = new Discipline { Name = "D", Abbreviation = "D" };
        context.Disciplines.Add(discipline);
        await context.SaveChangesAsync();

        var controller = new CoursesController(context, CreateStubAdapter());
        var dto = new CreateCourseDto { DisciplineId = discipline.Id, GroupId = 999, Year = 2024 };

        var actionResult = await controller.CreateCourse(dto);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task UpdateCourse_ReturnsNotFound_WhenCourseDoesNotExist()
    {
        using var context = GetDbContext();
        var controller = new CoursesController(context, CreateStubAdapter());
        var dto = new UpdateCourseDto { DisciplineId = 1, GroupId = 1, Year = 2024, IsActive = true };

        var result = await controller.UpdateCourse(999, dto);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateCourse_ReturnsBadRequest_WhenDisciplineNotFound()
    {
        using var context = GetDbContext();
        var discipline = new Discipline { Name = "D", Abbreviation = "D" };
        var group = new DomainGroup { Name = "G", ShortName = "G", YearStarted = 2024 };
        context.Disciplines.Add(discipline);
        context.Groups.Add(group);
        var course = new Course { Discipline = discipline, Group = group, Year = 2024, IsActive = true };
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var controller = new CoursesController(context, CreateStubAdapter());
        var dto = new UpdateCourseDto { DisciplineId = 999, GroupId = group.Id, Year = 2024, IsActive = true };

        var result = await controller.UpdateCourse(course.Id, dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateCourse_ReturnsBadRequest_WhenGroupNotFound()
    {
        using var context = GetDbContext();
        var discipline = new Discipline { Name = "D", Abbreviation = "D" };
        var group = new DomainGroup { Name = "G", ShortName = "G", YearStarted = 2024 };
        context.Disciplines.Add(discipline);
        context.Groups.Add(group);
        var course = new Course { Discipline = discipline, Group = group, Year = 2024, IsActive = true };
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var controller = new CoursesController(context, CreateStubAdapter());
        var dto = new UpdateCourseDto { DisciplineId = discipline.Id, GroupId = 999, Year = 2024, IsActive = true };

        var result = await controller.UpdateCourse(course.Id, dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateCourse_ReturnsNoContent_UpdatesAllFields()
    {
        using var context = GetDbContext();
        var discipline1 = new Discipline { Name = "D1", Abbreviation = "D1" };
        var discipline2 = new Discipline { Name = "D2", Abbreviation = "D2" };
        var group1 = new DomainGroup { Name = "G1", ShortName = "G1", YearStarted = 2024 };
        var group2 = new DomainGroup { Name = "G2", ShortName = "G2", YearStarted = 2024 };
        context.Disciplines.AddRange(discipline1, discipline2);
        context.Groups.AddRange(group1, group2);
        var course = new Course { Discipline = discipline1, Group = group1, Year = 2024, IsActive = true };
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var controller = new CoursesController(context, CreateStubAdapter());
        var dto = new UpdateCourseDto { DisciplineId = discipline2.Id, GroupId = group2.Id, Year = 2025, IsActive = false };

        var result = await controller.UpdateCourse(course.Id, dto);

        Assert.IsType<NoContentResult>(result);
        var updated = await context.Courses.FindAsync(course.Id);
        Assert.Equal(discipline2.Id, updated!.DisciplineId);
        Assert.Equal(group2.Id, updated.GroupId);
        Assert.Equal(2025, updated.Year);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task DeleteCourse_ReturnsNotFound_WhenCourseDoesNotExist()
    {
        using var context = GetDbContext();
        var controller = new CoursesController(context, CreateStubAdapter());

        var result = await controller.DeleteCourse(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteCourse_ReturnsNoContent_RemovesCourse()
    {
        using var context = GetDbContext();
        var discipline = new Discipline { Name = "D", Abbreviation = "D" };
        var group = new DomainGroup { Name = "G", ShortName = "G", YearStarted = 2024 };
        context.Disciplines.Add(discipline);
        context.Groups.Add(group);
        var course = new Course { Discipline = discipline, Group = group, Year = 2024, IsActive = true };
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var controller = new CoursesController(context, CreateStubAdapter());
        var result = await controller.DeleteCourse(course.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Null(await context.Courses.FindAsync(course.Id));
    }

    [Fact]
    public async Task GetProgress_ReturnsNotFound_WhenCourseDoesNotExist()
    {
        using var context = GetDbContext();
        var controller = new CoursesController(context, CreateStubAdapter());

        var actionResult = await controller.GetProgress(999);

        Assert.IsType<NotFoundResult>(actionResult.Result);
    }

    [Fact]
    public async Task GetProgress_ReturnsProgress_WithStudentsAndTasks()
    {
        using var context = GetDbContext();
        var discipline = new Discipline { Name = "Math", Abbreviation = "MTH" };
        var group = new DomainGroup { Name = "G", ShortName = "G", YearStarted = 2024 };
        context.Disciplines.Add(discipline);
        context.Groups.Add(group);
        var course = new Course { Discipline = discipline, Group = group, Year = 2024, IsActive = true };
        context.Courses.Add(course);
        context.Students.AddRange(
            new Student { FirstName = "John", LastName = "Doe", GroupId = group.Id },
            new Student { FirstName = "Alice", LastName = "Brown", GroupId = group.Id }
        );
        context.Tasks.AddRange(
            new DisciplineTask { DisciplineId = discipline.Id, Number = 2, Name = "Task B", GradingType = 1 },
            new DisciplineTask { DisciplineId = discipline.Id, Number = 1, Name = "Task A", GradingType = 1 }
        );
        await context.SaveChangesAsync();

        var controller = new CoursesController(context, CreateStubAdapter());
        var actionResult = await controller.GetProgress(course.Id);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var progress = Assert.IsType<CourseProgressDto>(ok.Value);
        Assert.Equal("Math", progress.DisciplineName);
        Assert.Equal("G", progress.GroupName);
        Assert.True(progress.IsActive);
        Assert.Equal(2, progress.Students.Count);
        Assert.Equal("Brown", progress.Students[0].LastName);
        Assert.Equal("Doe", progress.Students[1].LastName);
        Assert.Equal(2, progress.Tasks.Count);
        Assert.Equal(1, progress.Tasks[0].Number);
        Assert.Equal(2, progress.Tasks[1].Number);
        Assert.Empty(progress.Grades);
    }

    [Fact]
    public async Task GetProgress_ReturnsProgress_WithExistingGrades()
    {
        using var context = GetDbContext();
        var discipline = new Discipline { Name = "D", Abbreviation = "D" };
        var group = new DomainGroup { Name = "G", ShortName = "G", YearStarted = 2024 };
        context.Disciplines.Add(discipline);
        context.Groups.Add(group);
        var course = new Course { Discipline = discipline, Group = group, Year = 2024, IsActive = true };
        context.Courses.Add(course);
        var student = new Student { FirstName = "John", LastName = "Doe", GroupId = group.Id };
        var task = new DisciplineTask { DisciplineId = discipline.Id, Number = 1, Name = "Task", GradingType = 1 };
        context.Students.Add(student);
        context.Tasks.Add(task);
        await context.SaveChangesAsync();
        context.StudentGrades.Add(new StudentGrade { StudentId = student.Id, DisciplineTaskId = task.Id, CourseId = course.Id, Value = "1" });
        await context.SaveChangesAsync();

        var controller = new CoursesController(context, CreateStubAdapter());
        var actionResult = await controller.GetProgress(course.Id);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var progress = Assert.IsType<CourseProgressDto>(ok.Value);
        var gradeKey = $"{student.Id}_{task.Id}";
        Assert.True(progress.Grades.ContainsKey(gradeKey));
        Assert.Equal("1", progress.Grades[gradeKey]);
    }

    [Fact]
    public async Task GetProgress_ReturnsEmptyCollections_WhenNoStudentsOrTasks()
    {
        using var context = GetDbContext();
        var discipline = new Discipline { Name = "D", Abbreviation = "D" };
        var group = new DomainGroup { Name = "G", ShortName = "G", YearStarted = 2024 };
        context.Disciplines.Add(discipline);
        context.Groups.Add(group);
        var course = new Course { Discipline = discipline, Group = group, Year = 2024, IsActive = true };
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var controller = new CoursesController(context, CreateStubAdapter());
        var actionResult = await controller.GetProgress(course.Id);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var progress = Assert.IsType<CourseProgressDto>(ok.Value);
        Assert.Empty(progress.Students);
        Assert.Empty(progress.Tasks);
        Assert.Empty(progress.Grades);
    }

    [Fact]
    public async Task SaveGrades_ReturnsNotFound_WhenCourseDoesNotExist()
    {
        using var context = GetDbContext();
        var controller = new CoursesController(context, CreateStubAdapter());
        var dto = new BulkSaveGradesDto { Grades = new() };

        var result = await controller.SaveGrades(999, dto);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task SaveGrades_ReturnsNoContent_SavesNewGrade()
    {
        using var context = GetDbContext();
        var discipline = new Discipline { Name = "D", Abbreviation = "D" };
        var group = new DomainGroup { Name = "G", ShortName = "G", YearStarted = 2024 };
        context.Disciplines.Add(discipline);
        context.Groups.Add(group);
        var course = new Course { Discipline = discipline, Group = group, Year = 2024, IsActive = true };
        var student = new Student { FirstName = "John", LastName = "Doe", GroupId = group.Id };
        var task = new DisciplineTask { DisciplineId = discipline.Id, Number = 1, Name = "Task", GradingType = 1 };
        context.Courses.Add(course);
        context.Students.Add(student);
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        var controller = new CoursesController(context, CreateStubAdapter());
        var dto = new BulkSaveGradesDto
        {
            Grades = new List<GradeEntryDto>
            {
                new() { StudentId = student.Id, TaskId = task.Id, Value = "1" }
            }
        };

        var result = await controller.SaveGrades(course.Id, dto);

        Assert.IsType<NoContentResult>(result);
        var grade = await context.StudentGrades.FirstOrDefaultAsync(g => g.StudentId == student.Id && g.DisciplineTaskId == task.Id);
        Assert.NotNull(grade);
        Assert.Equal("1", grade.Value);
    }

    [Fact]
    public async Task SaveGrades_ReturnsNoContent_UpdatesExistingGrade()
    {
        using var context = GetDbContext();
        var discipline = new Discipline { Name = "D", Abbreviation = "D" };
        var group = new DomainGroup { Name = "G", ShortName = "G", YearStarted = 2024 };
        context.Disciplines.Add(discipline);
        context.Groups.Add(group);
        var course = new Course { Discipline = discipline, Group = group, Year = 2024, IsActive = true };
        var student = new Student { FirstName = "John", LastName = "Doe", GroupId = group.Id };
        var task = new DisciplineTask { DisciplineId = discipline.Id, Number = 1, Name = "Task", GradingType = 1 };
        context.Courses.Add(course);
        context.Students.Add(student);
        context.Tasks.Add(task);
        await context.SaveChangesAsync();
        context.StudentGrades.Add(new StudentGrade { StudentId = student.Id, DisciplineTaskId = task.Id, CourseId = course.Id, Value = "0" });
        await context.SaveChangesAsync();

        var controller = new CoursesController(context, CreateStubAdapter());
        var dto = new BulkSaveGradesDto
        {
            Grades = new List<GradeEntryDto>
            {
                new() { StudentId = student.Id, TaskId = task.Id, Value = "1" }
            }
        };

        var result = await controller.SaveGrades(course.Id, dto);

        Assert.IsType<NoContentResult>(result);
        var grade = await context.StudentGrades.FirstOrDefaultAsync(g => g.StudentId == student.Id && g.DisciplineTaskId == task.Id);
        Assert.NotNull(grade);
        Assert.Equal("1", grade.Value);
    }

    [Fact]
    public async Task SaveGrades_ReturnsBadRequest_WhenTaskNotFound()
    {
        using var context = GetDbContext();
        var discipline = new Discipline { Name = "D", Abbreviation = "D" };
        var group = new DomainGroup { Name = "G", ShortName = "G", YearStarted = 2024 };
        context.Disciplines.Add(discipline);
        context.Groups.Add(group);
        var course = new Course { Discipline = discipline, Group = group, Year = 2024, IsActive = true };
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var controller = new CoursesController(context, CreateStubAdapter());
        var dto = new BulkSaveGradesDto
        {
            Grades = new List<GradeEntryDto>
            {
                new() { StudentId = 1, TaskId = 999, Value = "1" }
            }
        };

        var result = await controller.SaveGrades(course.Id, dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SaveGrades_ReturnsBadRequest_WhenInvalidBinaryGrade()
    {
        using var context = GetDbContext();
        var discipline = new Discipline { Name = "D", Abbreviation = "D" };
        var group = new DomainGroup { Name = "G", ShortName = "G", YearStarted = 2024 };
        context.Disciplines.Add(discipline);
        context.Groups.Add(group);
        var course = new Course { Discipline = discipline, Group = group, Year = 2024, IsActive = true };
        var student = new Student { FirstName = "John", LastName = "Doe", GroupId = group.Id };
        var task = new DisciplineTask { DisciplineId = discipline.Id, Number = 1, Name = "Task", GradingType = 1 };
        context.Courses.Add(course);
        context.Students.Add(student);
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        var controller = new CoursesController(context, CreateStubAdapter());
        var dto = new BulkSaveGradesDto
        {
            Grades = new List<GradeEntryDto>
            {
                new() { StudentId = student.Id, TaskId = task.Id, Value = "2" }
            }
        };

        var result = await controller.SaveGrades(course.Id, dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SaveGrades_ReturnsBadRequest_WhenScoreExceedsMax()
    {
        using var context = GetDbContext();
        var discipline = new Discipline { Name = "D", Abbreviation = "D" };
        var group = new DomainGroup { Name = "G", ShortName = "G", YearStarted = 2024 };
        context.Disciplines.Add(discipline);
        context.Groups.Add(group);
        var course = new Course { Discipline = discipline, Group = group, Year = 2024, IsActive = true };
        var student = new Student { FirstName = "John", LastName = "Doe", GroupId = group.Id };
        var task = new DisciplineTask { DisciplineId = discipline.Id, Number = 1, Name = "Task", GradingType = 2, MaxScore = 10 };
        context.Courses.Add(course);
        context.Students.Add(student);
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        var controller = new CoursesController(context, CreateStubAdapter());
        var dto = new BulkSaveGradesDto
        {
            Grades = new List<GradeEntryDto>
            {
                new() { StudentId = student.Id, TaskId = task.Id, Value = "15" }
            }
        };

        var result = await controller.SaveGrades(course.Id, dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SaveGrades_ReturnsNoContent_WhenEmptyGrade()
    {
        using var context = GetDbContext();
        var discipline = new Discipline { Name = "D", Abbreviation = "D" };
        var group = new DomainGroup { Name = "G", ShortName = "G", YearStarted = 2024 };
        context.Disciplines.Add(discipline);
        context.Groups.Add(group);
        var course = new Course { Discipline = discipline, Group = group, Year = 2024, IsActive = true };
        var student = new Student { FirstName = "John", LastName = "Doe", GroupId = group.Id };
        var task = new DisciplineTask { DisciplineId = discipline.Id, Number = 1, Name = "Task", GradingType = 2, MaxScore = 10 };
        context.Courses.Add(course);
        context.Students.Add(student);
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        var controller = new CoursesController(context, CreateStubAdapter());
        var dto = new BulkSaveGradesDto
        {
            Grades = new List<GradeEntryDto>
            {
                new() { StudentId = student.Id, TaskId = task.Id, Value = null }
            }
        };

        var result = await controller.SaveGrades(course.Id, dto);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task SaveGrades_ReturnsNoContent_ValidBinaryGrades()
    {
        using var context = GetDbContext();
        var discipline = new Discipline { Name = "D", Abbreviation = "D" };
        var group = new DomainGroup { Name = "G", ShortName = "G", YearStarted = 2024 };
        context.Disciplines.Add(discipline);
        context.Groups.Add(group);
        var course = new Course { Discipline = discipline, Group = group, Year = 2024, IsActive = true };
        var student = new Student { FirstName = "John", LastName = "Doe", GroupId = group.Id };
        var task = new DisciplineTask { DisciplineId = discipline.Id, Number = 1, Name = "Task", GradingType = 1 };
        context.Courses.Add(course);
        context.Students.Add(student);
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        var controller = new CoursesController(context, CreateStubAdapter());
        var dto = new BulkSaveGradesDto
        {
            Grades = new List<GradeEntryDto>
            {
                new() { StudentId = student.Id, TaskId = task.Id, Value = "0" }
            }
        };

        var result = await controller.SaveGrades(course.Id, dto);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task SaveGrades_ReturnsNoContent_ValidScoreInRange()
    {
        using var context = GetDbContext();
        var discipline = new Discipline { Name = "D", Abbreviation = "D" };
        var group = new DomainGroup { Name = "G", ShortName = "G", YearStarted = 2024 };
        context.Disciplines.Add(discipline);
        context.Groups.Add(group);
        var course = new Course { Discipline = discipline, Group = group, Year = 2024, IsActive = true };
        var student = new Student { FirstName = "John", LastName = "Doe", GroupId = group.Id };
        var task = new DisciplineTask { DisciplineId = discipline.Id, Number = 1, Name = "Task", GradingType = 2, MaxScore = 10 };
        context.Courses.Add(course);
        context.Students.Add(student);
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        var controller = new CoursesController(context, CreateStubAdapter());
        var dto = new BulkSaveGradesDto
        {
            Grades = new List<GradeEntryDto>
            {
                new() { StudentId = student.Id, TaskId = task.Id, Value = "5" }
            }
        };

        var result = await controller.SaveGrades(course.Id, dto);

        Assert.IsType<NoContentResult>(result);
    }
}

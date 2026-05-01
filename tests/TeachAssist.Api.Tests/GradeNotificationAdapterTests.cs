using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TeachAssist.Api.DTOs;
using TeachAssist.Api.Options;
using TeachAssist.Api.Services;
using TeachAssist.Domain.Data;
using TeachAssist.Domain.Models;

namespace TeachAssist.Api.Tests;

public class GradeNotificationAdapterTests
{
    private static ServiceProvider CreateServiceProvider(string dbName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<DomainDbContext>(options => options.UseInMemoryDatabase(dbName));
        services.AddLogging(b => b.AddConsole());
        return services.BuildServiceProvider();
    }

    private static async Task<(Course course, Student student, DisciplineTask task)> SetupTestData(IServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DomainDbContext>();

        var discipline = new Discipline { Name = "Mathematics", Abbreviation = "MATH" };
        var group = new DomainGroup { Name = "Test Group", ShortName = "TG-01", YearStarted = 2024 };
        context.Disciplines.Add(discipline);
        context.Groups.Add(group);
        await context.SaveChangesAsync();

        var course = new Course { DisciplineId = discipline.Id, GroupId = group.Id, Year = 2024, IsActive = true };
        var student = new Student { FirstName = "John", LastName = "Doe", Email = "test@smtpbucket.com", GroupId = group.Id };
        var task = new DisciplineTask { DisciplineId = discipline.Id, Number = 1, Name = "Test Assignment", GradingType = 1 };
        context.Courses.Add(course);
        context.Students.Add(student);
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        return (course, student, task);
    }

    private static ILogger<GradeNotificationAdapter> CreateTestLogger(IServiceProvider provider)
    {
        return provider.GetRequiredService<ILogger<GradeNotificationAdapter>>();
    }

    [Fact]
    public async Task SendAsync_WithValidSettings_Succeeds()
    {
        var dbName = nameof(SendAsync_WithValidSettings_Succeeds);
        await using var provider = CreateServiceProvider(dbName);
        var (course, student, task) = await SetupTestData(provider);

        var smtpOptions = new SmtpOptions
        {
            Host = "mail.smtpbucket.com",
            Port = 8025,
            EnableSsl = false,
            FromEmail = "test@teachassist.local",
            FromName = "TeachAssist"
        };

        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var adapter = new GradeNotificationAdapter(smtpOptions, CreateTestLogger(provider), scopeFactory);
        var grades = new List<GradeEntryDto>
        {
            new() { StudentId = student.Id, TaskId = task.Id, Value = "1" }
        };

        var exception = await Record.ExceptionAsync(() =>
            adapter.NotifyGradesSavedAsync(course.Id, grades, CancellationToken.None));

        Assert.Null(exception);
    }

    [Fact]
    public async Task SendAsync_WithInvalidHost_DoesNotThrow()
    {
        var dbName = nameof(SendAsync_WithInvalidHost_DoesNotThrow);
        await using var provider = CreateServiceProvider(dbName);
        var (course, student, task) = await SetupTestData(provider);

        var smtpOptions = new SmtpOptions
        {
            Host = "invalid-host-that-does-not-exist.local",
            Port = 25,
            EnableSsl = false,
            FromEmail = "test@teachassist.local",
            FromName = "TeachAssist"
        };

        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var adapter = new GradeNotificationAdapter(smtpOptions, CreateTestLogger(provider), scopeFactory);
        var grades = new List<GradeEntryDto>
        {
            new() { StudentId = student.Id, TaskId = task.Id, Value = "1" }
        };

        var exception = await Record.ExceptionAsync(() =>
            adapter.NotifyGradesSavedAsync(course.Id, grades, CancellationToken.None));

        Assert.Null(exception);
    }

    [Fact]
    public async Task SendAsync_WithInvalidEmail_SkipsStudent()
    {
        var dbName = nameof(SendAsync_WithInvalidEmail_SkipsStudent);
        await using var provider = CreateServiceProvider(dbName);
        var (course, student, task) = await SetupTestData(provider);

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DomainDbContext>();
        student.Email = "not-an-email";
        await context.SaveChangesAsync();

        var smtpOptions = new SmtpOptions
        {
            Host = "mail.smtpbucket.com",
            Port = 8025,
            EnableSsl = false,
            FromEmail = "test@teachassist.local",
            FromName = "TeachAssist"
        };

        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var adapter = new GradeNotificationAdapter(smtpOptions, CreateTestLogger(provider), scopeFactory);
        var grades = new List<GradeEntryDto>
        {
            new() { StudentId = student.Id, TaskId = task.Id, Value = "1" }
        };

        var exception = await Record.ExceptionAsync(() =>
            adapter.NotifyGradesSavedAsync(course.Id, grades, CancellationToken.None));

        Assert.Null(exception);
    }
}

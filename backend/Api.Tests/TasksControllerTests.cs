using Microsoft.EntityFrameworkCore;
using TeachAssist.Api.Controllers;
using TeachAssist.Api.DTOs;
using TeachAssist.Domain.Data;
using TeachAssist.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace TeachAssist.Api.Tests;

public class TasksControllerTests
{
    private static DomainDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<DomainDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new DomainDbContext(options);
    }

    private static async Task<Discipline> CreateTestDiscipline(DomainDbContext context, int id = 1, string name = "Test Discipline")
    {
        var discipline = new Discipline { Id = id, Name = name, Abbreviation = "TD" };
        context.Disciplines.Add(discipline);
        await context.SaveChangesAsync();
        return discipline;
    }

    // ==================== GetTasks ====================

    [Fact]
    public async Task GetTasks_ReturnsEmptyList_WhenNoTasksExist()
    {
        await using var context = CreateInMemoryContext(nameof(GetTasks_ReturnsEmptyList_WhenNoTasksExist));
        await CreateTestDiscipline(context);
        var controller = new TasksController(context);

        var result = await controller.GetTasks(1, null);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var tasks = Assert.IsType<List<DisciplineTaskDto>>(okResult.Value);
        Assert.Empty(tasks);
    }

    [Fact]
    public async Task GetTasks_ReturnsTasksOrderedByNumber()
    {
        await using var context = CreateInMemoryContext(nameof(GetTasks_ReturnsTasksOrderedByNumber));
        var discipline = await CreateTestDiscipline(context);
        context.Tasks.AddRange(
            new DisciplineTask { DisciplineId = discipline.Id, Number = 3, Name = "Task C", GradingType = 1 },
            new DisciplineTask { DisciplineId = discipline.Id, Number = 1, Name = "Task A", GradingType = 1 },
            new DisciplineTask { DisciplineId = discipline.Id, Number = 2, Name = "Task B", GradingType = 1 }
        );
        await context.SaveChangesAsync();
        var controller = new TasksController(context);

        var result = await controller.GetTasks(1, null);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var tasks = Assert.IsType<List<DisciplineTaskDto>>(okResult.Value);
        Assert.Equal(3, tasks.Count);
        Assert.Equal(1, tasks[0].Number);
        Assert.Equal(2, tasks[1].Number);
        Assert.Equal(3, tasks[2].Number);
    }

    [Fact]
    public async Task GetTasks_FiltersBySearchTerm()
    {
        await using var context = CreateInMemoryContext(nameof(GetTasks_FiltersBySearchTerm));
        var discipline = await CreateTestDiscipline(context);
        context.Tasks.AddRange(
            new DisciplineTask { DisciplineId = discipline.Id, Number = 1, Name = "Lab 1", GradingType = 1 },
            new DisciplineTask { DisciplineId = discipline.Id, Number = 2, Name = "Lab 2", GradingType = 1 },
            new DisciplineTask { DisciplineId = discipline.Id, Number = 3, Name = "Homework", GradingType = 1 }
        );
        await context.SaveChangesAsync();
        var controller = new TasksController(context);

        var result = await controller.GetTasks(1, "Lab");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var tasks = Assert.IsType<List<DisciplineTaskDto>>(okResult.Value);
        Assert.Equal(2, tasks.Count);
        Assert.All(tasks, t => Assert.Contains("Lab", t.Name));
    }

    // ==================== CreateTask ====================

    [Fact]
    public async Task CreateTask_ReturnsCreated_WithValidBinaryDto()
    {
        await using var context = CreateInMemoryContext(nameof(CreateTask_ReturnsCreated_WithValidBinaryDto));
        await CreateTestDiscipline(context);
        var controller = new TasksController(context);
        var dto = new CreateDisciplineTaskDto { Name = "Binary Task", GradingType = 1 };

        var result = await controller.CreateTask(1, dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returned = Assert.IsType<DisciplineTaskDto>(createdResult.Value);
        Assert.Equal(1, returned.Number);
        Assert.Null(returned.MaxScore);
    }

    [Fact]
    public async Task CreateTask_ReturnsCreated_WithValidScoreDto()
    {
        await using var context = CreateInMemoryContext(nameof(CreateTask_ReturnsCreated_WithValidScoreDto));
        await CreateTestDiscipline(context);
        var controller = new TasksController(context);
        var dto = new CreateDisciplineTaskDto { Name = "Score Task", GradingType = 2, MaxScore = 100 };

        var result = await controller.CreateTask(1, dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returned = Assert.IsType<DisciplineTaskDto>(createdResult.Value);
        Assert.Equal(1, returned.Number);
        Assert.Equal(100, returned.MaxScore);
    }

    [Fact]
    public async Task CreateTask_ReturnsNotFound_WhenDisciplineNotFound()
    {
        await using var context = CreateInMemoryContext(nameof(CreateTask_ReturnsNotFound_WhenDisciplineNotFound));
        var controller = new TasksController(context);
        var dto = new CreateDisciplineTaskDto { Name = "Task", GradingType = 1 };

        var result = await controller.CreateTask(999, dto);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateTask_ReturnsBadRequest_WhenScoreGradingWithoutMaxScore()
    {
        await using var context = CreateInMemoryContext(nameof(CreateTask_ReturnsBadRequest_WhenScoreGradingWithoutMaxScore));
        await CreateTestDiscipline(context);
        var controller = new TasksController(context);
        var dto = new CreateDisciplineTaskDto { Name = "Task", GradingType = 2, MaxScore = null };

        var result = await controller.CreateTask(1, dto);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateTask_AutoIncrementsNumber()
    {
        await using var context = CreateInMemoryContext(nameof(CreateTask_AutoIncrementsNumber));
        var discipline = await CreateTestDiscipline(context);
        context.Tasks.AddRange(
            new DisciplineTask { DisciplineId = discipline.Id, Number = 1, Name = "Task 1", GradingType = 1 },
            new DisciplineTask { DisciplineId = discipline.Id, Number = 2, Name = "Task 2", GradingType = 1 }
        );
        await context.SaveChangesAsync();
        var controller = new TasksController(context);
        var dto = new CreateDisciplineTaskDto { Name = "Task 3", GradingType = 1 };

        var result = await controller.CreateTask(1, dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returned = Assert.IsType<DisciplineTaskDto>(createdResult.Value);
        Assert.Equal(3, returned.Number);
    }

    [Fact]
    public async Task CreateTask_IgnoresMaxScore_ForBinaryGrading()
    {
        await using var context = CreateInMemoryContext(nameof(CreateTask_IgnoresMaxScore_ForBinaryGrading));
        await CreateTestDiscipline(context);
        var controller = new TasksController(context);
        var dto = new CreateDisciplineTaskDto { Name = "Task", GradingType = 1, MaxScore = 50 };

        var result = await controller.CreateTask(1, dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returned = Assert.IsType<DisciplineTaskDto>(createdResult.Value);
        Assert.Null(returned.MaxScore);
    }

    // ==================== UpdateTask ====================

    [Fact]
    public async Task UpdateTask_ReturnsNotFound_WhenTaskNotFound()
    {
        await using var context = CreateInMemoryContext(nameof(UpdateTask_ReturnsNotFound_WhenTaskNotFound));
        await CreateTestDiscipline(context);
        var controller = new TasksController(context);
        var dto = new UpdateDisciplineTaskDto { Name = "Updated", GradingType = 1 };

        var result = await controller.UpdateTask(1, 999, dto);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateTask_ReturnsNotFound_WhenTaskInDifferentDiscipline()
    {
        await using var context = CreateInMemoryContext(nameof(UpdateTask_ReturnsNotFound_WhenTaskInDifferentDiscipline));
        var discipline1 = await CreateTestDiscipline(context, id: 1);
        var discipline2 = await CreateTestDiscipline(context, id: 2);
        var task = new DisciplineTask { DisciplineId = discipline2.Id, Number = 1, Name = "Task", GradingType = 1 };
        context.Tasks.Add(task);
        await context.SaveChangesAsync();
        var controller = new TasksController(context);
        var dto = new UpdateDisciplineTaskDto { Name = "Updated", GradingType = 1 };

        var result = await controller.UpdateTask(1, task.Id, dto);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateTask_ReturnsUpdatedTask()
    {
        await using var context = CreateInMemoryContext(nameof(UpdateTask_ReturnsUpdatedTask));
        var discipline = await CreateTestDiscipline(context);
        var task = new DisciplineTask { DisciplineId = discipline.Id, Number = 1, Name = "Original", GradingType = 1 };
        context.Tasks.Add(task);
        await context.SaveChangesAsync();
        var controller = new TasksController(context);
        var dto = new UpdateDisciplineTaskDto { Name = "Updated", GradingType = 2, MaxScore = 10 };

        var result = await controller.UpdateTask(1, task.Id, dto);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<DisciplineTaskDto>(okResult.Value);
        Assert.Equal("Updated", returned.Name);
        Assert.Equal(2, returned.GradingType);
        Assert.Equal(10, returned.MaxScore);
    }

    [Fact]
    public async Task UpdateTask_ReturnsBadRequest_WhenScoreGradingWithoutMaxScore()
    {
        await using var context = CreateInMemoryContext(nameof(UpdateTask_ReturnsBadRequest_WhenScoreGradingWithoutMaxScore));
        var discipline = await CreateTestDiscipline(context);
        var task = new DisciplineTask { DisciplineId = discipline.Id, Number = 1, Name = "Task", GradingType = 1 };
        context.Tasks.Add(task);
        await context.SaveChangesAsync();
        var controller = new TasksController(context);
        var dto = new UpdateDisciplineTaskDto { Name = "Updated", GradingType = 2, MaxScore = null };

        var result = await controller.UpdateTask(1, task.Id, dto);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateTask_ClearsMaxScore_WhenSwitchingToBinary()
    {
        await using var context = CreateInMemoryContext(nameof(UpdateTask_ClearsMaxScore_WhenSwitchingToBinary));
        var discipline = await CreateTestDiscipline(context);
        var task = new DisciplineTask { DisciplineId = discipline.Id, Number = 1, Name = "Task", GradingType = 2, MaxScore = 100 };
        context.Tasks.Add(task);
        await context.SaveChangesAsync();
        var controller = new TasksController(context);
        var dto = new UpdateDisciplineTaskDto { Name = "Updated", GradingType = 1 };

        var result = await controller.UpdateTask(1, task.Id, dto);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<DisciplineTaskDto>(okResult.Value);
        Assert.Null(returned.MaxScore);
    }

    // ==================== DeleteTask ====================

    [Fact]
    public async Task DeleteTask_ReturnsNotFound_WhenTaskNotFound()
    {
        await using var context = CreateInMemoryContext(nameof(DeleteTask_ReturnsNotFound_WhenTaskNotFound));
        await CreateTestDiscipline(context);
        var controller = new TasksController(context);

        var result = await controller.DeleteTask(1, 999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteTask_ReturnsNoContent_WhenSingleTaskDeleted()
    {
        await using var context = CreateInMemoryContext(nameof(DeleteTask_ReturnsNoContent_WhenSingleTaskDeleted));
        var discipline = await CreateTestDiscipline(context);
        var task = new DisciplineTask { DisciplineId = discipline.Id, Number = 1, Name = "Task", GradingType = 1 };
        context.Tasks.Add(task);
        await context.SaveChangesAsync();
        var controller = new TasksController(context);

        var result = await controller.DeleteTask(1, task.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(await context.Tasks.ToListAsync());
    }

    [Fact]
    public async Task DeleteTask_ReordersRemainingTasks()
    {
        await using var context = CreateInMemoryContext(nameof(DeleteTask_ReordersRemainingTasks));
        var discipline = await CreateTestDiscipline(context);
        var task1 = new DisciplineTask { DisciplineId = discipline.Id, Number = 1, Name = "Task 1", GradingType = 1 };
        var task2 = new DisciplineTask { DisciplineId = discipline.Id, Number = 2, Name = "Task 2", GradingType = 1 };
        var task3 = new DisciplineTask { DisciplineId = discipline.Id, Number = 3, Name = "Task 3", GradingType = 1 };
        context.Tasks.AddRange(task1, task2, task3);
        await context.SaveChangesAsync();
        var controller = new TasksController(context);

        var result = await controller.DeleteTask(1, task2.Id);

        Assert.IsType<NoContentResult>(result);
        var remaining = await context.Tasks.OrderBy(t => t.Number).ToListAsync();
        Assert.Equal(2, remaining.Count);
        Assert.Equal(1, remaining[0].Number);
        Assert.Equal(2, remaining[1].Number);
    }

    [Fact]
    public async Task DeleteTask_DoesNotAffectOtherDisciplines()
    {
        await using var context = CreateInMemoryContext(nameof(DeleteTask_DoesNotAffectOtherDisciplines));
        var discipline1 = await CreateTestDiscipline(context, id: 1);
        var discipline2 = await CreateTestDiscipline(context, id: 2);
        var task1 = new DisciplineTask { DisciplineId = discipline1.Id, Number = 1, Name = "Task 1", GradingType = 1 };
        context.Tasks.Add(task1);
        var task2 = new DisciplineTask { DisciplineId = discipline2.Id, Number = 1, Name = "Task 2", GradingType = 1 };
        context.Tasks.Add(task2);
        await context.SaveChangesAsync();
        var controller = new TasksController(context);

        await controller.DeleteTask(1, task1.Id);

        var otherDisciplineTask = await context.Tasks.FindAsync(task2.Id);
        Assert.NotNull(otherDisciplineTask);
        Assert.Equal(1, otherDisciplineTask.Number);
    }

    // ==================== ChangePriority ====================

    [Fact]
    public async Task ChangePriority_ReturnsNotFound_WhenTaskNotFound()
    {
        await using var context = CreateInMemoryContext(nameof(ChangePriority_ReturnsNotFound_WhenTaskNotFound));
        await CreateTestDiscipline(context);
        var controller = new TasksController(context);

        var result = await controller.ChangePriority(1, 999, "up");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task ChangePriority_SwapsNumbers_Up()
    {
        await using var context = CreateInMemoryContext(nameof(ChangePriority_SwapsNumbers_Up));
        var discipline = await CreateTestDiscipline(context);
        var task1 = new DisciplineTask { DisciplineId = discipline.Id, Number = 1, Name = "Task 1", GradingType = 1 };
        var task2 = new DisciplineTask { DisciplineId = discipline.Id, Number = 2, Name = "Task 2", GradingType = 1 };
        context.Tasks.AddRange(task1, task2);
        await context.SaveChangesAsync();
        var controller = new TasksController(context);

        var result = await controller.ChangePriority(1, task2.Id, "up");

        Assert.IsType<NoContentResult>(result);
        var updatedTask1 = await context.Tasks.FindAsync(task1.Id);
        var updatedTask2 = await context.Tasks.FindAsync(task2.Id);
        Assert.Equal(2, updatedTask1!.Number);
        Assert.Equal(1, updatedTask2!.Number);
    }

    [Fact]
    public async Task ChangePriority_SwapsNumbers_Down()
    {
        await using var context = CreateInMemoryContext(nameof(ChangePriority_SwapsNumbers_Down));
        var discipline = await CreateTestDiscipline(context);
        var task1 = new DisciplineTask { DisciplineId = discipline.Id, Number = 1, Name = "Task 1", GradingType = 1 };
        var task2 = new DisciplineTask { DisciplineId = discipline.Id, Number = 2, Name = "Task 2", GradingType = 1 };
        context.Tasks.AddRange(task1, task2);
        await context.SaveChangesAsync();
        var controller = new TasksController(context);

        var result = await controller.ChangePriority(1, task1.Id, "down");

        Assert.IsType<NoContentResult>(result);
        var updatedTask1 = await context.Tasks.FindAsync(task1.Id);
        var updatedTask2 = await context.Tasks.FindAsync(task2.Id);
        Assert.Equal(2, updatedTask1!.Number);
        Assert.Equal(1, updatedTask2!.Number);
    }

    [Fact]
    public async Task ChangePriority_ReturnsBadRequest_FirstTaskUp()
    {
        await using var context = CreateInMemoryContext(nameof(ChangePriority_ReturnsBadRequest_FirstTaskUp));
        var discipline = await CreateTestDiscipline(context);
        var task1 = new DisciplineTask { DisciplineId = discipline.Id, Number = 1, Name = "Task 1", GradingType = 1 };
        var task2 = new DisciplineTask { DisciplineId = discipline.Id, Number = 2, Name = "Task 2", GradingType = 1 };
        var task3 = new DisciplineTask { DisciplineId = discipline.Id, Number = 3, Name = "Task 3", GradingType = 1 };
        context.Tasks.AddRange(task1, task2, task3);
        await context.SaveChangesAsync();
        var controller = new TasksController(context);

        var result = await controller.ChangePriority(1, task1.Id, "up");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ChangePriority_ReturnsBadRequest_LastTaskDown()
    {
        await using var context = CreateInMemoryContext(nameof(ChangePriority_ReturnsBadRequest_LastTaskDown));
        var discipline = await CreateTestDiscipline(context);
        var task1 = new DisciplineTask { DisciplineId = discipline.Id, Number = 1, Name = "Task 1", GradingType = 1 };
        var task2 = new DisciplineTask { DisciplineId = discipline.Id, Number = 2, Name = "Task 2", GradingType = 1 };
        var task3 = new DisciplineTask { DisciplineId = discipline.Id, Number = 3, Name = "Task 3", GradingType = 1 };
        context.Tasks.AddRange(task1, task2, task3);
        await context.SaveChangesAsync();
        var controller = new TasksController(context);

        var result = await controller.ChangePriority(1, task3.Id, "down");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ChangePriority_UpdatesTimestamps()
    {
        await using var context = CreateInMemoryContext(nameof(ChangePriority_UpdatesTimestamps));
        var discipline = await CreateTestDiscipline(context);
        var task1 = new DisciplineTask { DisciplineId = discipline.Id, Number = 1, Name = "Task 1", GradingType = 1, UpdatedAt = DateTime.UtcNow.AddDays(-1) };
        var task2 = new DisciplineTask { DisciplineId = discipline.Id, Number = 2, Name = "Task 2", GradingType = 1, UpdatedAt = DateTime.UtcNow.AddDays(-1) };
        context.Tasks.AddRange(task1, task2);
        await context.SaveChangesAsync();
        var controller = new TasksController(context);

        await controller.ChangePriority(1, task2.Id, "up");

        var updatedTask1 = await context.Tasks.FindAsync(task1.Id);
        var updatedTask2 = await context.Tasks.FindAsync(task2.Id);
        Assert.True(updatedTask1!.UpdatedAt > DateTime.UtcNow.AddMinutes(-1));
        Assert.True(updatedTask2!.UpdatedAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task ChangePriority_DoesNotAffectOtherDisciplines()
    {
        await using var context = CreateInMemoryContext(nameof(ChangePriority_DoesNotAffectOtherDisciplines));
        var discipline1 = await CreateTestDiscipline(context, id: 1);
        var discipline2 = await CreateTestDiscipline(context, id: 2);
        var task1a = new DisciplineTask { DisciplineId = discipline1.Id, Number = 1, Name = "Task 1a", GradingType = 1 };
        var task1b = new DisciplineTask { DisciplineId = discipline1.Id, Number = 2, Name = "Task 1b", GradingType = 1 };
        var task2a = new DisciplineTask { DisciplineId = discipline2.Id, Number = 1, Name = "Task 2a", GradingType = 1 };
        var task2b = new DisciplineTask { DisciplineId = discipline2.Id, Number = 2, Name = "Task 2b", GradingType = 1 };
        context.Tasks.AddRange(task1a, task1b, task2a, task2b);
        await context.SaveChangesAsync();
        var controller = new TasksController(context);

        await controller.ChangePriority(1, task1b.Id, "up");

        var otherTask2a = await context.Tasks.FindAsync(task2a.Id);
        var otherTask2b = await context.Tasks.FindAsync(task2b.Id);
        Assert.Equal(1, otherTask2a!.Number);
        Assert.Equal(2, otherTask2b!.Number);
    }
}

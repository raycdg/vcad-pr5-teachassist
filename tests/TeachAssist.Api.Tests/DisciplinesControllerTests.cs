using Microsoft.EntityFrameworkCore;
using TeachAssist.Api.Controllers;
using TeachAssist.Api.DTOs;
using TeachAssist.Domain.Data;
using TeachAssist.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace TeachAssist.Api.Tests;

public class DisciplinesControllerTests
{
    private static DomainDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<DomainDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new DomainDbContext(options);
    }

    // ==================== GetDisciplines ====================

    [Fact]
    public async Task GetDisciplines_ReturnsEmptyList_WhenNoDisciplinesExist()
    {
        await using var context = CreateInMemoryContext(nameof(GetDisciplines_ReturnsEmptyList_WhenNoDisciplinesExist));
        var controller = new DisciplinesController(context);

        var result = await controller.GetDisciplines();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var disciplines = Assert.IsType<List<DisciplineDto>>(okResult.Value);
        Assert.Empty(disciplines);
    }

    [Fact]
    public async Task GetDisciplines_ReturnsAllDisciplinesOrderedByName()
    {
        await using var context = CreateInMemoryContext(nameof(GetDisciplines_ReturnsAllDisciplinesOrderedByName));
        context.Disciplines.AddRange(
            new Discipline { Name = "Math", Abbreviation = "MTH" },
            new Discipline { Name = "Algebra", Abbreviation = "ALG" },
            new Discipline { Name = "Physics", Abbreviation = "PHY" }
        );
        await context.SaveChangesAsync();
        var controller = new DisciplinesController(context);

        var result = await controller.GetDisciplines();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var disciplines = Assert.IsType<List<DisciplineDto>>(okResult.Value);
        Assert.Equal(3, disciplines.Count);
        Assert.Equal("Algebra", disciplines[0].Name);
        Assert.Equal("Math", disciplines[1].Name);
        Assert.Equal("Physics", disciplines[2].Name);
    }

    // ==================== GetDiscipline ====================

    [Fact]
    public async Task GetDiscipline_ReturnsNotFound_WhenDisciplineDoesNotExist()
    {
        await using var context = CreateInMemoryContext(nameof(GetDiscipline_ReturnsNotFound_WhenDisciplineDoesNotExist));
        var controller = new DisciplinesController(context);

        var result = await controller.GetDiscipline(999);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetDiscipline_ReturnsDiscipline_WhenExists()
    {
        await using var context = CreateInMemoryContext(nameof(GetDiscipline_ReturnsDiscipline_WhenExists));
        var discipline = new Discipline { Name = "Math", Abbreviation = "MTH" };
        context.Disciplines.Add(discipline);
        await context.SaveChangesAsync();
        var controller = new DisciplinesController(context);

        var result = await controller.GetDiscipline(discipline.Id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<DisciplineDto>(okResult.Value);
        Assert.Equal("Math", dto.Name);
        Assert.Equal("MTH", dto.Abbreviation);
    }

    // ==================== CreateDiscipline ====================

    [Fact]
    public async Task CreateDiscipline_ReturnsCreated_WithValidDto()
    {
        await using var context = CreateInMemoryContext(nameof(CreateDiscipline_ReturnsCreated_WithValidDto));
        var controller = new DisciplinesController(context);
        var dto = new CreateDisciplineDto { Name = "Math", Abbreviation = "MTH" };

        var result = await controller.CreateDiscipline(dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returned = Assert.IsType<DisciplineDto>(createdResult.Value);
        Assert.True(returned.Id > 0);
        Assert.Equal("Math", returned.Name);
        Assert.Equal("MTH", returned.Abbreviation);
    }

    [Fact]
    public async Task CreateDiscipline_ReturnsBadRequest_WhenNameExists()
    {
        await using var context = CreateInMemoryContext(nameof(CreateDiscipline_ReturnsBadRequest_WhenNameExists));
        context.Disciplines.Add(new Discipline { Name = "Math", Abbreviation = "MTH" });
        await context.SaveChangesAsync();
        var controller = new DisciplinesController(context);
        var dto = new CreateDisciplineDto { Name = "Math", Abbreviation = "NEW" };

        var result = await controller.CreateDiscipline(dto);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateDiscipline_AllowsDuplicateAbbreviation()
    {
        await using var context = CreateInMemoryContext(nameof(CreateDiscipline_AllowsDuplicateAbbreviation));
        context.Disciplines.Add(new Discipline { Name = "Math", Abbreviation = "MTH" });
        await context.SaveChangesAsync();
        var controller = new DisciplinesController(context);
        var dto = new CreateDisciplineDto { Name = "Advanced Math", Abbreviation = "MTH" };

        var result = await controller.CreateDiscipline(dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal("Advanced Math", ((DisciplineDto)createdResult.Value).Name);
    }

    [Fact]
    public async Task CreateDiscipline_SavesCreatedAtAndUpdatedAt()
    {
        await using var context = CreateInMemoryContext(nameof(CreateDiscipline_SavesCreatedAtAndUpdatedAt));
        var controller = new DisciplinesController(context);
        var dto = new CreateDisciplineDto { Name = "Math", Abbreviation = "MTH" };

        var result = await controller.CreateDiscipline(dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returned = Assert.IsType<DisciplineDto>(createdResult.Value);
        Assert.True(returned.CreatedAt > DateTime.MinValue);
        Assert.True(returned.UpdatedAt > DateTime.MinValue);
    }

    [Fact]
    public async Task CreateDiscipline_ReturnsBadRequest_WhenDuplicateNameConcurrent()
    {
        await using var context = CreateInMemoryContext(nameof(CreateDiscipline_ReturnsBadRequest_WhenDuplicateNameConcurrent));
        context.Disciplines.Add(new Discipline { Name = "Math", Abbreviation = "MTH" });
        await context.SaveChangesAsync();
        var controller = new DisciplinesController(context);
        var dto = new CreateDisciplineDto { Name = "Math", Abbreviation = "NEW" };

        var task1 = controller.CreateDiscipline(dto);
        var task2 = controller.CreateDiscipline(dto);

        await Task.WhenAll(task1, task2);

        var results = new[] { task1.Result.Result, task2.Result.Result };
        var hasBadRequest = results.Any(r => r is BadRequestObjectResult);

        Assert.True(hasBadRequest, "At least one request should return BadRequest to prevent duplicate in race condition");
    }

    // ==================== UpdateDiscipline ====================

    [Fact]
    public async Task UpdateDiscipline_ReturnsNotFound_WhenDisciplineDoesNotExist()
    {
        await using var context = CreateInMemoryContext(nameof(UpdateDiscipline_ReturnsNotFound_WhenDisciplineDoesNotExist));
        var controller = new DisciplinesController(context);
        var dto = new UpdateDisciplineDto { Name = "Updated", Abbreviation = "UPD" };

        var result = await controller.UpdateDiscipline(999, dto);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateDiscipline_ReturnsUpdatedDiscipline()
    {
        await using var context = CreateInMemoryContext(nameof(UpdateDiscipline_ReturnsUpdatedDiscipline));
        var discipline = new Discipline { Name = "Math", Abbreviation = "MTH" };
        context.Disciplines.Add(discipline);
        await context.SaveChangesAsync();
        var controller = new DisciplinesController(context);
        var dto = new UpdateDisciplineDto { Name = "Algebra", Abbreviation = "ALG" };

        var result = await controller.UpdateDiscipline(discipline.Id, dto);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<DisciplineDto>(okResult.Value);
        Assert.Equal("Algebra", returned.Name);
        Assert.Equal("ALG", returned.Abbreviation);
    }

    [Fact]
    public async Task UpdateDiscipline_ReturnsBadRequest_WhenNameExists()
    {
        await using var context = CreateInMemoryContext(nameof(UpdateDiscipline_ReturnsBadRequest_WhenNameExists));
        var math = new Discipline { Id = 1, Name = "Math", Abbreviation = "MTH" };
        var algebra = new Discipline { Id = 2, Name = "Algebra", Abbreviation = "ALG" };
        context.Disciplines.AddRange(math, algebra);
        await context.SaveChangesAsync();
        var controller = new DisciplinesController(context);
        var dto = new UpdateDisciplineDto { Name = "Algebra", Abbreviation = "MTH-NEW" };

        var result = await controller.UpdateDiscipline(1, dto);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateDiscipline_AllowsSameNameForSameDiscipline()
    {
        await using var context = CreateInMemoryContext(nameof(UpdateDiscipline_AllowsSameNameForSameDiscipline));
        var discipline = new Discipline { Name = "Math", Abbreviation = "MTH" };
        context.Disciplines.Add(discipline);
        await context.SaveChangesAsync();
        var controller = new DisciplinesController(context);
        var dto = new UpdateDisciplineDto { Name = "Math", Abbreviation = "MTH-NEW" };

        var result = await controller.UpdateDiscipline(discipline.Id, dto);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<DisciplineDto>(okResult.Value);
        Assert.Equal("Math", returned.Name);
        Assert.Equal("MTH-NEW", returned.Abbreviation);
    }

    [Fact]
    public async Task UpdateDiscipline_UpdatesTimestamp()
    {
        await using var context = CreateInMemoryContext(nameof(UpdateDiscipline_UpdatesTimestamp));
        var discipline = new Discipline { Name = "Math", Abbreviation = "MTH", UpdatedAt = DateTime.UtcNow.AddDays(-1) };
        context.Disciplines.Add(discipline);
        await context.SaveChangesAsync();
        var controller = new DisciplinesController(context);
        var dto = new UpdateDisciplineDto { Name = "Math", Abbreviation = "MTH" };

        await controller.UpdateDiscipline(discipline.Id, dto);

        var updated = await context.Disciplines.FindAsync(discipline.Id);
        Assert.True(updated!.UpdatedAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task UpdateDiscipline_ReturnsBadRequest_WhenDuplicateNameConcurrent()
    {
        await using var context = CreateInMemoryContext(nameof(UpdateDiscipline_ReturnsBadRequest_WhenDuplicateNameConcurrent));
        var disciplineA = new Discipline { Id = 1, Name = "Discipline A", Abbreviation = "DA" };
        var disciplineB = new Discipline { Id = 2, Name = "Discipline B", Abbreviation = "DB" };
        context.Disciplines.AddRange(disciplineA, disciplineB);
        await context.SaveChangesAsync();
        var controller = new DisciplinesController(context);
        var dto = new UpdateDisciplineDto { Name = "Discipline B", Abbreviation = "DA-UPDATED" };

        var task1 = controller.UpdateDiscipline(1, dto);
        var task2 = controller.UpdateDiscipline(1, dto);

        await Task.WhenAll(task1, task2);

        var results = new[] { task1.Result.Result, task2.Result.Result };
        var hasBadRequest = results.Any(r => r is BadRequestObjectResult);

        Assert.True(hasBadRequest, "At least one request should return BadRequest to prevent duplicate in race condition");
    }

    // ==================== DeleteDiscipline ====================

    [Fact]
    public async Task DeleteDiscipline_ReturnsNotFound_WhenDisciplineDoesNotExist()
    {
        await using var context = CreateInMemoryContext(nameof(DeleteDiscipline_ReturnsNotFound_WhenDisciplineDoesNotExist));
        var controller = new DisciplinesController(context);

        var result = await controller.DeleteDiscipline(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteDiscipline_ReturnsNoContent_WhenDisciplineExists()
    {
        await using var context = CreateInMemoryContext(nameof(DeleteDiscipline_ReturnsNoContent_WhenDisciplineExists));
        var discipline = new Discipline { Name = "Math", Abbreviation = "MTH" };
        context.Disciplines.Add(discipline);
        await context.SaveChangesAsync();
        var controller = new DisciplinesController(context);

        var result = await controller.DeleteDiscipline(discipline.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(await context.Disciplines.ToListAsync());
    }
}

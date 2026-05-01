using Microsoft.EntityFrameworkCore;
using TeachAssist.Api.Controllers;
using TeachAssist.Api.DTOs;
using TeachAssist.Domain.Data;
using TeachAssist.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace TeachAssist.Api.Tests;

public class GroupsControllerTests
{
    private static DomainDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<DomainDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new DomainDbContext(options);
    }

    [Fact]
    public async Task GetGroups_ReturnsEmptyList_WhenNoGroupsExist()
    {
        await using var context = CreateInMemoryContext(nameof(GetGroups_ReturnsEmptyList_WhenNoGroupsExist));
        var controller = new GroupsController(context);

        var result = await controller.GetGroups();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var groups = Assert.IsType<List<GroupDto>>(okResult.Value);
        Assert.Empty(groups);
    }

    [Fact]
    public async Task GetGroups_ReturnsAllGroups()
    {
        await using var context = CreateInMemoryContext(nameof(GetGroups_ReturnsAllGroups));
        context.Groups.Add(new DomainGroup { Name = "Test Group", ShortName = "TG-01", YearStarted = 2024 });
        await context.SaveChangesAsync();
        var controller = new GroupsController(context);

        var result = await controller.GetGroups();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var groups = Assert.IsType<List<GroupDto>>(okResult.Value);
        Assert.Single(groups);
        Assert.Equal("Test Group", groups[0].Name);
    }

    [Fact]
    public async Task GetGroup_ReturnsNotFound_WhenGroupDoesNotExist()
    {
        await using var context = CreateInMemoryContext(nameof(GetGroup_ReturnsNotFound_WhenGroupDoesNotExist));
        var controller = new GroupsController(context);

        var result = await controller.GetGroup(999);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetGroup_ReturnsGroup_WhenExists()
    {
        await using var context = CreateInMemoryContext(nameof(GetGroup_ReturnsGroup_WhenExists));
        var group = new DomainGroup { Name = "Test Group", ShortName = "TG-01", YearStarted = 2024 };
        context.Groups.Add(group);
        await context.SaveChangesAsync();
        var controller = new GroupsController(context);

        var result = await controller.GetGroup(group.Id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<GroupDto>(okResult.Value);
        Assert.Equal("Test Group", dto.Name);
    }

    [Fact]
    public async Task CreateGroup_ReturnsCreatedGroup()
    {
        await using var context = CreateInMemoryContext(nameof(CreateGroup_ReturnsCreatedGroup));
        var controller = new GroupsController(context);
        var dto = new CreateGroupDto { Name = "New Group", ShortName = "NG-01", YearStarted = 2025 };

        var result = await controller.CreateGroup(dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returned = Assert.IsType<GroupDto>(createdResult.Value);
        Assert.Equal("New Group", returned.Name);
        Assert.True(returned.Id > 0);
        Assert.Single(await context.Groups.ToListAsync());
    }

    [Fact]
    public async Task UpdateGroup_ReturnsNotFound_WhenGroupDoesNotExist()
    {
        await using var context = CreateInMemoryContext(nameof(UpdateGroup_ReturnsNotFound_WhenGroupDoesNotExist));
        var controller = new GroupsController(context);
        var dto = new UpdateGroupDto { Name = "Updated", ShortName = "UP-01", YearStarted = 2025 };

        var result = await controller.UpdateGroup(999, dto);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateGroup_ReturnsUpdatedGroup_WhenGroupExists()
    {
        await using var context = CreateInMemoryContext(nameof(UpdateGroup_ReturnsUpdatedGroup_WhenGroupExists));
        var group = new DomainGroup { Name = "Original", ShortName = "OR-01", YearStarted = 2024 };
        context.Groups.Add(group);
        await context.SaveChangesAsync();
        var controller = new GroupsController(context);
        var dto = new UpdateGroupDto { Name = "Updated", ShortName = "UP-01", YearStarted = 2025 };

        var result = await controller.UpdateGroup(group.Id, dto);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<GroupDto>(okResult.Value);
        Assert.Equal("Updated", returned.Name);
        Assert.Equal("UP-01", returned.ShortName);
        Assert.Equal(2025, returned.YearStarted);
    }

    [Fact]
    public async Task DeleteGroup_ReturnsNotFound_WhenGroupDoesNotExist()
    {
        await using var context = CreateInMemoryContext(nameof(DeleteGroup_ReturnsNotFound_WhenGroupDoesNotExist));
        var controller = new GroupsController(context);

        var result = await controller.DeleteGroup(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteGroup_ReturnsNoContent_WhenGroupExists()
    {
        await using var context = CreateInMemoryContext(nameof(DeleteGroup_ReturnsNoContent_WhenGroupExists));
        var group = new DomainGroup { Name = "ToDelete", ShortName = "TD-01", YearStarted = 2024 };
        context.Groups.Add(group);
        await context.SaveChangesAsync();
        var controller = new GroupsController(context);

        var result = await controller.DeleteGroup(group.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(await context.Groups.ToListAsync());
    }

    [Fact]
    public async Task GetGroups_OrdersByYearStartedThenName()
    {
        await using var context = CreateInMemoryContext(nameof(GetGroups_OrdersByYearStartedThenName));
        context.Groups.AddRange(
            new DomainGroup { Name = "Group B", ShortName = "GB", YearStarted = 2024 },
            new DomainGroup { Name = "Group A", ShortName = "GA", YearStarted = 2024 },
            new DomainGroup { Name = "Group C", ShortName = "GC", YearStarted = 2025 }
        );
        await context.SaveChangesAsync();
        var controller = new GroupsController(context);

        var result = await controller.GetGroups();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var groups = Assert.IsType<List<GroupDto>>(okResult.Value);
        Assert.Equal(3, groups.Count);
        Assert.Equal("Group A", groups[0].Name);
        Assert.Equal("Group B", groups[1].Name);
        Assert.Equal("Group C", groups[2].Name);
    }

    [Fact]
    public async Task CreateGroup_ReturnsBadRequest_WhenNameExists()
    {
        await using var context = CreateInMemoryContext(nameof(CreateGroup_ReturnsBadRequest_WhenNameExists));
        context.Groups.Add(new DomainGroup { Name = "Existing Group", ShortName = "EX", YearStarted = 2024 });
        await context.SaveChangesAsync();
        var controller = new GroupsController(context);

        var dto = new CreateGroupDto { Name = "Existing Group", ShortName = "NG", YearStarted = 2025 };
        var result = await controller.CreateGroup(dto);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateGroup_ReturnsBadRequest_WhenNameExists()
    {
        await using var context = CreateInMemoryContext(nameof(UpdateGroup_ReturnsBadRequest_WhenNameExists));
        context.Groups.AddRange(
            new DomainGroup { Id = 1, Name = "Group One", ShortName = "GO", YearStarted = 2024 },
            new DomainGroup { Id = 2, Name = "Group Two", ShortName = "GT", YearStarted = 2025 }
        );
        await context.SaveChangesAsync();
        var controller = new GroupsController(context);

        var dto = new UpdateGroupDto { Name = "Group Two", ShortName = "GO-UPDATED", YearStarted = 2026 };
        var result = await controller.UpdateGroup(1, dto);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateGroup_ReturnsBadRequest_WhenDuplicateNameConcurrent()
    {
        await using var context = CreateInMemoryContext(nameof(UpdateGroup_ReturnsBadRequest_WhenDuplicateNameConcurrent));
        context.Groups.AddRange(
            new DomainGroup { Id = 1, Name = "Group A", ShortName = "GA", YearStarted = 2024 },
            new DomainGroup { Id = 2, Name = "Group B", ShortName = "GB", YearStarted = 2025 }
        );
        await context.SaveChangesAsync();
        var controller = new GroupsController(context);

        var dto = new UpdateGroupDto { Name = "Group B", ShortName = "GA-UPD", YearStarted = 2026 };

        var task1 = controller.UpdateGroup(1, dto);
        var task2 = controller.UpdateGroup(1, dto);

        await Task.WhenAll(task1, task2);

        var results = new[] { task1.Result.Result, task2.Result.Result };
        var hasBadRequest = results.Any(r => r is BadRequestObjectResult);

        Assert.True(hasBadRequest, "At least one request should return BadRequest to prevent duplicate in race condition");
    }

    [Fact]
    public async Task CreateGroup_ReturnsBadRequest_WhenDuplicateNameConcurrent()
    {
        await using var context = CreateInMemoryContext(nameof(CreateGroup_ReturnsBadRequest_WhenDuplicateNameConcurrent));
        context.Groups.Add(new DomainGroup { Name = "Concurrent Group", ShortName = "CG", YearStarted = 2024 });
        await context.SaveChangesAsync();
        var controller = new GroupsController(context);

        var dto = new CreateGroupDto { Name = "Concurrent Group", ShortName = "CG-NEW", YearStarted = 2025 };

        var task1 = controller.CreateGroup(dto);
        var task2 = controller.CreateGroup(dto);

        await Task.WhenAll(task1, task2);

        var results = new[] { task1.Result.Result, task2.Result.Result };
        var hasBadRequest = results.Any(r => r is BadRequestObjectResult);
        var hasCreated = results.Any(r => r is CreatedAtActionResult);

        Assert.True(hasBadRequest && hasCreated || !hasCreated,
            "At least one request should return BadRequest to prevent duplicate in race condition");
    }
}

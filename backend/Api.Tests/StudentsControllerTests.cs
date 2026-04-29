using Microsoft.EntityFrameworkCore;
using TeachAssist.Api.Controllers;
using TeachAssist.Api.DTOs;
using TeachAssist.Domain.Data;
using TeachAssist.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace TeachAssist.Api.Tests;

public class StudentsControllerTests
{
    private static DomainDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<DomainDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new DomainDbContext(options);
    }

    private static async Task<DomainGroup> CreateTestGroup(DomainDbContext context)
    {
        var group = new DomainGroup { Name = "Test Group", ShortName = "TG-01", YearStarted = 2024 };
        context.Groups.Add(group);
        await context.SaveChangesAsync();
        return group;
    }

    [Fact]
    public async Task GetStudentsByGroup_ReturnsNotFound_WhenGroupDoesNotExist()
    {
        await using var context = CreateInMemoryContext(nameof(GetStudentsByGroup_ReturnsNotFound_WhenGroupDoesNotExist));
        var controller = new StudentsController(context);

        var result = await controller.GetStudentsByGroup(999);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetStudentsByGroup_ReturnsEmptyList_WhenNoStudentsInGroup()
    {
        await using var context = CreateInMemoryContext(nameof(GetStudentsByGroup_ReturnsEmptyList_WhenNoStudentsInGroup));
        var group = await CreateTestGroup(context);
        var controller = new StudentsController(context);

        var result = await controller.GetStudentsByGroup(group.Id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var students = Assert.IsType<List<StudentDto>>(okResult.Value);
        Assert.Empty(students);
    }

    [Fact]
    public async Task GetStudentsByGroup_ReturnsAllStudents_WhenStudentsExist()
    {
        await using var context = CreateInMemoryContext(nameof(GetStudentsByGroup_ReturnsAllStudents_WhenStudentsExist));
        var group = await CreateTestGroup(context);
        context.Students.AddRange(
            new Student { FirstName = "John", LastName = "Doe", GroupId = group.Id },
            new Student { FirstName = "Jane", LastName = "Smith", GroupId = group.Id }
        );
        await context.SaveChangesAsync();
        var controller = new StudentsController(context);

        var result = await controller.GetStudentsByGroup(group.Id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var students = Assert.IsType<List<StudentDto>>(okResult.Value);
        Assert.Equal(2, students.Count);
    }

    [Fact]
    public async Task GetStudentsByGroup_ReturnsOnlyStudentsFromGroup()
    {
        await using var context = CreateInMemoryContext(nameof(GetStudentsByGroup_ReturnsOnlyStudentsFromGroup));
        var group1 = await CreateTestGroup(context);
        var group2 = new DomainGroup { Name = "Group 2", ShortName = "G2", YearStarted = 2025 };
        context.Groups.Add(group2);
        await context.SaveChangesAsync();
        context.Students.AddRange(
            new Student { FirstName = "InGroup1", LastName = "Student", GroupId = group1.Id },
            new Student { FirstName = "InGroup2", LastName = "Student", GroupId = group2.Id }
        );
        await context.SaveChangesAsync();
        var controller = new StudentsController(context);

        var result = await controller.GetStudentsByGroup(group1.Id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var students = Assert.IsType<List<StudentDto>>(okResult.Value);
        Assert.Single(students);
        Assert.Equal("InGroup1", students[0].FirstName);
    }

    [Fact]
    public async Task GetStudent_ReturnsNotFound_WhenStudentDoesNotExist()
    {
        await using var context = CreateInMemoryContext(nameof(GetStudent_ReturnsNotFound_WhenStudentDoesNotExist));
        var controller = new StudentsController(context);

        var result = await controller.GetStudent(999);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetStudent_ReturnsStudent_WhenExists()
    {
        await using var context = CreateInMemoryContext(nameof(GetStudent_ReturnsStudent_WhenExists));
        var group = await CreateTestGroup(context);
        var student = new Student { FirstName = "John", LastName = "Doe", GroupId = group.Id };
        context.Students.Add(student);
        await context.SaveChangesAsync();
        var controller = new StudentsController(context);

        var result = await controller.GetStudent(student.Id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<StudentDto>(okResult.Value);
        Assert.Equal("John", dto.FirstName);
        Assert.Equal("Doe", dto.LastName);
    }

    [Fact]
    public async Task CreateStudent_ReturnsBadRequest_WhenGroupDoesNotExist()
    {
        await using var context = CreateInMemoryContext(nameof(CreateStudent_ReturnsBadRequest_WhenGroupDoesNotExist));
        var controller = new StudentsController(context);
        var dto = new CreateStudentDto { FirstName = "John", LastName = "Doe", GroupId = 999 };

        var result = await controller.CreateStudent(dto);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateStudent_ReturnsCreatedStudent()
    {
        await using var context = CreateInMemoryContext(nameof(CreateStudent_ReturnsCreatedStudent));
        var group = await CreateTestGroup(context);
        var controller = new StudentsController(context);
        var dto = new CreateStudentDto { FirstName = "John", LastName = "Doe", Email = "john@test.com", GroupId = group.Id };

        var result = await controller.CreateStudent(dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returned = Assert.IsType<StudentDto>(createdResult.Value);
        Assert.Equal("John", returned.FirstName);
        Assert.Equal("Doe", returned.LastName);
        Assert.Equal("john@test.com", returned.Email);
        Assert.True(returned.Id > 0);
    }

    [Fact]
    public async Task UpdateStudent_ReturnsNotFound_WhenStudentDoesNotExist()
    {
        await using var context = CreateInMemoryContext(nameof(UpdateStudent_ReturnsNotFound_WhenStudentDoesNotExist));
        var controller = new StudentsController(context);
        var dto = new UpdateStudentDto { FirstName = "Updated", LastName = "Name" };

        var result = await controller.UpdateStudent(999, dto);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateStudent_ReturnsUpdatedStudent()
    {
        await using var context = CreateInMemoryContext(nameof(UpdateStudent_ReturnsUpdatedStudent));
        var group = await CreateTestGroup(context);
        var student = new Student { FirstName = "Original", LastName = "Name", GroupId = group.Id };
        context.Students.Add(student);
        await context.SaveChangesAsync();
        var controller = new StudentsController(context);
        var dto = new UpdateStudentDto { FirstName = "Updated", LastName = "NewName", Email = "updated@test.com" };

        var result = await controller.UpdateStudent(student.Id, dto);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<StudentDto>(okResult.Value);
        Assert.Equal("Updated", returned.FirstName);
        Assert.Equal("NewName", returned.LastName);
        Assert.Equal("updated@test.com", returned.Email);
    }

    [Fact]
    public async Task DeleteStudent_ReturnsNotFound_WhenStudentDoesNotExist()
    {
        await using var context = CreateInMemoryContext(nameof(DeleteStudent_ReturnsNotFound_WhenStudentDoesNotExist));
        var controller = new StudentsController(context);

        var result = await controller.DeleteStudent(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteStudent_ReturnsNoContent_WhenStudentExists()
    {
        await using var context = CreateInMemoryContext(nameof(DeleteStudent_ReturnsNoContent_WhenStudentExists));
        var group = await CreateTestGroup(context);
        var student = new Student { FirstName = "ToDelete", LastName = "Student", GroupId = group.Id };
        context.Students.Add(student);
        await context.SaveChangesAsync();
        var controller = new StudentsController(context);

        var result = await controller.DeleteStudent(student.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(await context.Students.ToListAsync());
    }

    [Fact]
    public async Task GetStudentsByGroup_OrdersByLastNameThenFirstName()
    {
        await using var context = CreateInMemoryContext(nameof(GetStudentsByGroup_OrdersByLastNameThenFirstName));
        var group = await CreateTestGroup(context);
        context.Students.AddRange(
            new Student { FirstName = "John", LastName = "Brown", GroupId = group.Id },
            new Student { FirstName = "Alice", LastName = "Brown", GroupId = group.Id },
            new Student { FirstName = "John", LastName = "Adams", GroupId = group.Id }
        );
        await context.SaveChangesAsync();
        var controller = new StudentsController(context);

        var result = await controller.GetStudentsByGroup(group.Id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var students = Assert.IsType<List<StudentDto>>(okResult.Value);
        Assert.Equal(3, students.Count);
        Assert.Equal("Adams", students[0].LastName);
        Assert.Equal("Brown", students[1].LastName);
        Assert.Equal("Alice", students[1].FirstName);
        Assert.Equal("Brown", students[2].LastName);
        Assert.Equal("John", students[2].FirstName);
    }
}

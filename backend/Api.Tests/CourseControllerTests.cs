using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using TeachAssist.Api.Controllers;
using TeachAssist.Api.DTOs;
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

        var controller = new CoursesController(context);
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

        var controller = new CoursesController(context);
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

        var controller = new CoursesController(context);
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

        var controller = new CoursesController(context);
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

        var controller = new CoursesController(context);
        var dto = new BulkSaveGradesDto { Grades = new() };

        var result = await controller.SaveGrades(course.Id, dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}

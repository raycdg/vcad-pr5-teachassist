using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeachAssist.Api.DTOs;
using TeachAssist.Domain.Data;
using TeachAssist.Domain.Models;

namespace TeachAssist.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly DomainDbContext _context;

    public CoursesController(DomainDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CourseDto>>> GetCourses([FromQuery] bool showAll = false)
    {
        var query = _context.Courses
            .Include(c => c.Discipline)
            .Include(c => c.Group)
            .AsQueryable();

        if (!showAll)
        {
            query = query.Where(c => c.IsActive);
        }

        var courses = await query
            .OrderBy(c => c.Year)
            .ThenBy(c => c.Discipline.Name)
            .Select(c => MapToDto(c))
            .ToListAsync();

        return Ok(courses);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CourseDto>> GetCourse(int id)
    {
        var course = await _context.Courses
            .Include(c => c.Discipline)
            .Include(c => c.Group)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course == null) return NotFound();
        return Ok(MapToDto(course));
    }

    [HttpPost]
    public async Task<ActionResult<CourseDto>> CreateCourse(CreateCourseDto dto)
    {
        var disciplineExists = await _context.Disciplines.AnyAsync(d => d.Id == dto.DisciplineId);
        var groupExists = await _context.Groups.AnyAsync(g => g.Id == dto.GroupId);

        if (!disciplineExists || !groupExists)
            return BadRequest("Discipline or Group not found");

        var course = new Course
        {
            DisciplineId = dto.DisciplineId,
            GroupId = dto.GroupId,
            Year = dto.Year,
            IsActive = true
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        var created = await _context.Courses
            .Include(c => c.Discipline)
            .Include(c => c.Group)
            .FirstAsync(c => c.Id == course.Id);

        return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, MapToDto(created));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCourse(int id, UpdateCourseDto dto)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null) return NotFound();

        var disciplineExists = await _context.Disciplines.AnyAsync(d => d.Id == dto.DisciplineId);
        var groupExists = await _context.Groups.AnyAsync(g => g.Id == dto.GroupId);

        if (!disciplineExists || !groupExists)
            return BadRequest("Discipline or Group not found");

        course.DisciplineId = dto.DisciplineId;
        course.GroupId = dto.GroupId;
        course.Year = dto.Year;
        course.IsActive = dto.IsActive;
        course.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id}/toggle-status")]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null) return NotFound();

        course.IsActive = !course.IsActive;
        course.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null) return NotFound();

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/progress")]
    public async Task<ActionResult<CourseProgressDto>> GetProgress(int id)
    {
        var course = await _context.Courses
            .Include(c => c.Discipline)
            .Include(c => c.Group)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course == null) return NotFound();

        var students = await _context.Students
            .Where(s => s.GroupId == course.GroupId)
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .Select(s => new StudentProgressDto
            {
                Id = s.Id,
                FirstName = s.FirstName,
                LastName = s.LastName
            })
            .ToListAsync();

        var tasks = await _context.Tasks
            .Where(t => t.DisciplineId == course.DisciplineId)
            .OrderBy(t => t.Number)
            .Select(t => new TaskProgressDto
            {
                Id = t.Id,
                Number = t.Number,
                Name = t.Name,
                GradingType = t.GradingType,
                MaxScore = t.MaxScore
            })
            .ToListAsync();

        var existingGrades = await _context.StudentGrades
            .Where(g => g.CourseId == id)
            .ToListAsync();

        var gradesDict = existingGrades.ToDictionary(g => $"{g.StudentId}_{g.DisciplineTaskId}", g => g.Value);

        var progress = new CourseProgressDto
        {
            CourseId = course.Id,
            DisciplineName = course.Discipline.Name,
            GroupName = course.Group.Name,
            IsActive = course.IsActive,
            Students = students,
            Tasks = tasks
        };

        return Ok(progress);
    }

    [HttpPost("{id}/grades")]
    public async Task<IActionResult> SaveGrades(int id, BulkSaveGradesDto dto)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null) return NotFound();

        if (!course.IsActive)
            return BadRequest("Cannot save grades for inactive course");

        foreach (var entry in dto.Grades)
        {
            var existing = await _context.StudentGrades
                .FirstOrDefaultAsync(g => g.CourseId == id &&
                                         g.StudentId == entry.StudentId &&
                                         g.DisciplineTaskId == entry.TaskId);

            if (existing != null)
            {
                existing.Value = entry.Value;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.StudentGrades.Add(new StudentGrade
                {
                    StudentId = entry.StudentId,
                    DisciplineTaskId = entry.TaskId,
                    CourseId = id,
                    Value = entry.Value
                });
            }
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static CourseDto MapToDto(Course c) => new()
    {
        Id = c.Id,
        DisciplineId = c.DisciplineId,
        DisciplineName = c.Discipline.Name,
        GroupId = c.GroupId,
        GroupName = c.Group.Name,
        Year = c.Year,
        IsActive = c.IsActive,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };
}

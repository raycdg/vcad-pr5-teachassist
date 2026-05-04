using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeachAssist.Api.Authorization;
using TeachAssist.Api.DTOs;
using TeachAssist.Api.Models;
using TeachAssist.Api.Services;
using TeachAssist.Domain.Data;
using TeachAssist.Domain.Models;

namespace TeachAssist.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireTeacher")]
public class CoursesController : ControllerBase
{
    private readonly DomainDbContext _context;
    private readonly GradeNotificationAdapter _notificationAdapter;
    private readonly UserManager<AppUser> _userManager;
    private readonly IAuthorizationService _authorization;
    private readonly ILogger<CoursesController>? _logger;

    public CoursesController(
        DomainDbContext context,
        GradeNotificationAdapter notificationAdapter,
        UserManager<AppUser> userManager,
        IAuthorizationService authorization,
        ILogger<CoursesController>? logger = null)
    {
        _context = context;
        _notificationAdapter = notificationAdapter;
        _userManager = userManager;
        _authorization = authorization;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CourseDto>>> GetCourses([FromQuery] bool showAll = false)
    {
        var user = await _userManager.GetUserAsync(User);
        var isManagerOrAdmin = await _userManager.IsInRoleAsync(user!, "Manager") ||
                              await _userManager.IsInRoleAsync(user!, "Admin");

        var query = _context.Courses
            .Include(c => c.Discipline)
            .Include(c => c.Group)
            .AsQueryable();

        if (!showAll)
        {
            query = query.Where(c => c.IsActive);
        }

        // Teachers see only their courses
        if (!isManagerOrAdmin)
        {
            var teacherCourses = _context.CourseTeachers
                .Where(ct => ct.TeacherId == user!.Id)
                .Select(ct => ct.CourseId);
            query = query.Where(c => teacherCourses.Contains(c.Id));
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

        // Check if teacher can access this course
        var requirement = new ResourceAccessRequirement(ResourceType.Course, id);
        var authResult = await _authorization.AuthorizeAsync(User, null, new[] { requirement });
        if (!authResult.Succeeded && !(await _userManager.IsInRoleAsync((await _userManager.GetUserAsync(User))!, "Manager") ||
                                        await _userManager.IsInRoleAsync((await _userManager.GetUserAsync(User))!, "Admin")))
            return Forbid();

        return Ok(MapToDto(course));
    }

    [HttpPost]
    public async Task<ActionResult<CourseDto>> CreateCourse(CreateCourseDto dto)
    {
        // Teachers can create courses on any discipline, so we don't restrict discipline access
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

        // Automatically assign the creator teacher to the course
        var user = await _userManager.GetUserAsync(User);
        if (user != null)
        {
            _context.CourseTeachers.Add(new CourseTeacher
            {
                CourseId = course.Id,
                TeacherId = user.Id
            });
            await _context.SaveChangesAsync();
        }

        var created = await _context.Courses
            .Include(c => c.Discipline)
            .Include(c => c.Group)
            .FirstAsync(c => c.Id == course.Id);

        return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, MapToDto(created));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCourse(int id, UpdateCourseDto dto)
    {
        // Check if teacher can edit this course
        var requirement = new ResourceAccessRequirement(ResourceType.Course, id);
        var authResult = await _authorization.AuthorizeAsync(User, null, new[] { requirement });
        if (!authResult.Succeeded)
            return Forbid();

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
        // Check if teacher can toggle status of this course
        var requirement = new ResourceAccessRequirement(ResourceType.Course, id);
        var authResult = await _authorization.AuthorizeAsync(User, null, new[] { requirement });
        if (!authResult.Succeeded)
            return Forbid();

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
        // Check if teacher can delete this course
        var requirement = new ResourceAccessRequirement(ResourceType.Course, id);
        var authResult = await _authorization.AuthorizeAsync(User, null, new[] { requirement });
        if (!authResult.Succeeded)
            return Forbid();

        var course = await _context.Courses.FindAsync(id);
        if (course == null) return NotFound();

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/progress")]
    public async Task<ActionResult<CourseProgressDto>> GetProgress(int id)
    {
        // Check if teacher can access progress of this course
        var requirement = new ResourceAccessRequirement(ResourceType.Course, id);
        var authResult = await _authorization.AuthorizeAsync(User, null, new[] { requirement });
        if (!authResult.Succeeded && !(await _userManager.IsInRoleAsync((await _userManager.GetUserAsync(User))!, "Manager") ||
                                        await _userManager.IsInRoleAsync((await _userManager.GetUserAsync(User))!, "Admin")))
            return Forbid();

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
            Tasks = tasks,
            Grades = gradesDict
        };

        return Ok(progress);
    }

    [HttpPost("{id}/grades")]
    public async Task<IActionResult> SaveGrades(int id, BulkSaveGradesDto dto)
    {
        // Check if teacher can save grades for this course
        var requirement = new ResourceAccessRequirement(ResourceType.Course, id);
        var authResult = await _authorization.AuthorizeAsync(User, null, new[] { requirement });
        if (!authResult.Succeeded)
            return Forbid();

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

            var task = await _context.Tasks.FindAsync(entry.TaskId);
            if (task == null)
                return BadRequest($"Task with id {entry.TaskId} not found");

            if (!IsValidGrade(entry.Value, task))
                return BadRequest($"Invalid grade value '{entry.Value}' for task '{task.Name}' (grading type: {task.GradingType})");

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

        _ = NotifyGradesSavedSafeAsync(id, dto.Grades);

        return NoContent();
    }

    private async Task NotifyGradesSavedSafeAsync(int courseId, List<GradeEntryDto> grades)
    {
        try
        {
            await _notificationAdapter.NotifyGradesSavedAsync(courseId, grades, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to send grade notifications for course {CourseId}", courseId);
        }
    }

    [HttpPost("{id}/assign-teacher")]
    [Authorize(Policy = "RequireManager")]
    public async Task<IActionResult> AssignTeacher(int id, [FromBody] AssignTeacherDto dto)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null)
            return NotFound(new { message = $"Course with id {id} not found." });

        var teacher = await _userManager.FindByIdAsync(dto.TeacherId);
        if (teacher == null)
            return BadRequest(new { message = "Teacher not found." });

        var exists = await _context.CourseTeachers
            .AnyAsync(ct => ct.CourseId == id && ct.TeacherId == dto.TeacherId);

        if (exists)
            return BadRequest(new { message = "Teacher already assigned to this course." });

        _context.CourseTeachers.Add(new CourseTeacher
        {
            CourseId = id,
            TeacherId = dto.TeacherId
        });

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/teachers")]
    public async Task<ActionResult<IEnumerable<TeacherDto>>> GetCourseTeachers(int id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null)
            return NotFound(new { message = $"Course with id {id} not found." });

        var teacherIds = await _context.CourseTeachers
            .Where(ct => ct.CourseId == id)
            .Select(ct => ct.TeacherId)
            .ToListAsync();

        var teachers = await _userManager.Users
            .Where(u => teacherIds.Contains(u.Id) && !u.IsDeleted)
            .Select(u => new TeacherDto { Id = u.Id, Email = u.Email ?? string.Empty })
            .ToListAsync();

        return Ok(teachers);
    }

    [HttpDelete("{id}/teachers/{teacherId}")]
    [Authorize(Policy = "RequireManager")]
    public async Task<IActionResult> RemoveTeacher(int id, string teacherId)
    {
        var assignment = await _context.CourseTeachers
            .FirstOrDefaultAsync(ct => ct.CourseId == id && ct.TeacherId == teacherId);

        if (assignment == null)
            return NotFound(new { message = "Teacher not assigned to this course." });

        _context.CourseTeachers.Remove(assignment);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static bool IsValidGrade(string? value, DisciplineTask task)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        if (task.GradingType == 1)
        {
            return value is "0" or "1";
        }

        if (task.GradingType == 2)
        {
            return int.TryParse(value, out var score) && score >= 0 && score <= (task.MaxScore ?? 0);
        }

        return false;
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

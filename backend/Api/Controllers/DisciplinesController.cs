using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeachAssist.Api.Authorization;
using TeachAssist.Api.DTOs;
using TeachAssist.Api.Models;
using TeachAssist.Domain.Data;
using TeachAssist.Domain.Models;

namespace TeachAssist.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireTeacher")]
public class DisciplinesController : ControllerBase
{
    private readonly DomainDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly IAuthorizationService _authorization;

    public DisciplinesController(DomainDbContext context, UserManager<AppUser> userManager, IAuthorizationService authorization)
    {
        _context = context;
        _userManager = userManager;
        _authorization = authorization;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DisciplineDto>>> GetDisciplines()
    {
        var user = await _userManager.GetUserAsync(User);
        var isManagerOrAdmin = await _userManager.IsInRoleAsync(user!, "Manager") ||
                              await _userManager.IsInRoleAsync(user!, "Admin");

        var query = _context.Disciplines.AsQueryable();

        // Teachers see all disciplines (read-only for those not assigned)
        // Manager and Admin see all

        var disciplines = await query
            .OrderBy(d => d.Name)
            .Select(d => MapToDto(d))
            .ToListAsync();
        return Ok(disciplines);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DisciplineDto>> GetDiscipline(int id)
    {
        var discipline = await _context.Disciplines.FindAsync(id);
        if (discipline == null)
            return NotFound(new { message = $"Discipline with id {id} not found." });
        return Ok(MapToDto(discipline));
    }

    [HttpPost]
    public async Task<ActionResult<DisciplineDto>> CreateDiscipline([FromBody] CreateDisciplineDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var uniquenessError = await ValidateDisciplineNameUniqueness(dto.Name, null);
        if (uniquenessError != null)
            return uniquenessError;

        var discipline = new Discipline
        {
            Name = dto.Name,
            Abbreviation = dto.Abbreviation
        };

        _context.Disciplines.Add(discipline);
        await _context.SaveChangesAsync();

        // Automatically assign the creator teacher to the discipline
        var user = await _userManager.GetUserAsync(User);
        if (user != null)
        {
            _context.DisciplineTeachers.Add(new DisciplineTeacher
            {
                DisciplineId = discipline.Id,
                TeacherId = user.Id
            });
            await _context.SaveChangesAsync();
        }

        return CreatedAtAction(nameof(GetDiscipline), new { id = discipline.Id }, MapToDto(discipline));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<DisciplineDto>> UpdateDiscipline(int id, [FromBody] UpdateDisciplineDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var discipline = await _context.Disciplines.FindAsync(id);
        if (discipline == null)
            return NotFound(new { message = $"Discipline with id {id} not found." });

        // Check if teacher can edit this discipline
        var requirement = new ResourceAccessRequirement(ResourceType.Discipline, id);
        var authResult = await _authorization.AuthorizeAsync(User, null, new[] { requirement });
        if (!authResult.Succeeded)
            return Forbid();

        var uniquenessError = await ValidateDisciplineNameUniqueness(dto.Name, id);
        if (uniquenessError != null)
            return uniquenessError;

        discipline.Name = dto.Name;
        discipline.Abbreviation = dto.Abbreviation;
        discipline.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(MapToDto(discipline));
    }

    private async Task<ActionResult<DisciplineDto>?> ValidateDisciplineNameUniqueness(string name, int? excludeId)
    {
        var exists = excludeId.HasValue
            ? await _context.Disciplines.AnyAsync(d => d.Name == name && d.Id != excludeId.Value)
            : await _context.Disciplines.AnyAsync(d => d.Name == name);

        if (exists)
            return BadRequest(new { message = "Discipline with this name already exists." });

        return null;
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDiscipline(int id)
    {
        var discipline = await _context.Disciplines.FindAsync(id);
        if (discipline == null)
            return NotFound(new { message = $"Discipline with id {id} not found." });

        // Check if teacher can delete this discipline
        var requirement = new ResourceAccessRequirement(ResourceType.Discipline, id);
        var authResult = await _authorization.AuthorizeAsync(User, null, new[] { requirement });
        if (!authResult.Succeeded)
            return Forbid();

        _context.Disciplines.Remove(discipline);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/assign-teacher")]
    [Authorize(Policy = "RequireManager")]
    public async Task<IActionResult> AssignTeacher(int id, [FromBody] AssignTeacherDto dto)
    {
        var discipline = await _context.Disciplines.FindAsync(id);
        if (discipline == null)
            return NotFound(new { message = $"Discipline with id {id} not found." });

        var teacher = await _userManager.FindByIdAsync(dto.TeacherId);
        if (teacher == null)
            return BadRequest(new { message = "Teacher not found." });

        var exists = await _context.DisciplineTeachers
            .AnyAsync(dt => dt.DisciplineId == id && dt.TeacherId == dto.TeacherId);

        if (exists)
            return BadRequest(new { message = "Teacher already assigned to this discipline." });

        _context.DisciplineTeachers.Add(new DisciplineTeacher
        {
            DisciplineId = id,
            TeacherId = dto.TeacherId
        });

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/teachers")]
    public async Task<ActionResult<IEnumerable<TeacherDto>>> GetDisciplineTeachers(int id)
    {
        var discipline = await _context.Disciplines.FindAsync(id);
        if (discipline == null)
            return NotFound(new { message = $"Discipline with id {id} not found." });

        var teacherIds = await _context.DisciplineTeachers
            .Where(dt => dt.DisciplineId == id)
            .Select(dt => dt.TeacherId)
            .ToListAsync();

        var teachers = new List<TeacherDto>();
        foreach (var tid in teacherIds)
        {
            var user = await _userManager.FindByIdAsync(tid);
            if (user != null && !user.IsDeleted)
                teachers.Add(new TeacherDto { Id = user.Id, Email = user.Email ?? string.Empty });
        }

        return Ok(teachers);
    }

    [HttpDelete("{id}/teachers/{teacherId}")]
    [Authorize(Policy = "RequireManager")]
    public async Task<IActionResult> RemoveTeacher(int id, string teacherId)
    {
        var assignment = await _context.DisciplineTeachers
            .FirstOrDefaultAsync(dt => dt.DisciplineId == id && dt.TeacherId == teacherId);

        if (assignment == null)
            return NotFound(new { message = "Teacher not assigned to this discipline." });

        _context.DisciplineTeachers.Remove(assignment);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static DisciplineDto MapToDto(Discipline d) => new()
    {
        Id = d.Id,
        Name = d.Name,
        Abbreviation = d.Abbreviation,
        CreatedAt = d.CreatedAt,
        UpdatedAt = d.UpdatedAt
    };
}

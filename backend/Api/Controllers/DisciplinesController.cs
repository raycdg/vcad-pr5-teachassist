using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeachAssist.Api.DTOs;
using TeachAssist.Domain.Data;
using TeachAssist.Domain.Models;

namespace TeachAssist.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DisciplinesController : ControllerBase
{
    private readonly DomainDbContext _context;

    public DisciplinesController(DomainDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DisciplineDto>>> GetDisciplines()
    {
        var disciplines = await _context.Disciplines
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

        _context.Disciplines.Remove(discipline);
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

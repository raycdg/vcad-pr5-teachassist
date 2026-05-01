using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeachAssist.Api.DTOs;
using TeachAssist.Domain.Data;
using TeachAssist.Domain.Models;

namespace TeachAssist.Api.Controllers;

[ApiController]
[Route("api/disciplines/{disciplineId}/[controller]")]
[Authorize(Policy = "RequireTeacher")]
public class TasksController : ControllerBase
{
    private readonly DomainDbContext _context;

    public TasksController(DomainDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DisciplineTaskDto>>> GetTasks(int disciplineId, [FromQuery] string? search)
    {
        var query = _context.Tasks
            .Where(t => t.DisciplineId == disciplineId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Name.Contains(search));

        var tasks = await query
            .OrderBy(t => t.Number)
            .Select(t => MapToDto(t))
            .ToListAsync();

        return Ok(tasks);
    }

    [HttpPost]
    public async Task<ActionResult<DisciplineTaskDto>> CreateTask(int disciplineId, [FromBody] CreateDisciplineTaskDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var disciplineExists = await _context.Disciplines.AnyAsync(d => d.Id == disciplineId);
        if (!disciplineExists)
            return NotFound(new { message = $"Discipline with id {disciplineId} not found." });

        if (dto.GradingType == 2 && !dto.MaxScore.HasValue)
            return BadRequest(new { message = "MaxScore is required for score grading type." });

        var nextNumber = await _context.Tasks
            .Where(t => t.DisciplineId == disciplineId)
            .AnyAsync()
                ? await _context.Tasks
                    .Where(t => t.DisciplineId == disciplineId)
                    .MaxAsync(t => t.Number) + 1
                : 1;

        var task = new DisciplineTask
        {
            DisciplineId = disciplineId,
            Number = nextNumber,
            Name = dto.Name,
            GradingType = dto.GradingType,
            MaxScore = dto.GradingType == 2 ? dto.MaxScore : null
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTasks), new { disciplineId, id = task.Id }, MapToDto(task));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<DisciplineTaskDto>> UpdateTask(int disciplineId, int id, [FromBody] UpdateDisciplineTaskDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.DisciplineId == disciplineId);

        if (task == null)
            return NotFound(new { message = $"Task with id {id} not found." });

        if (dto.GradingType == 2 && !dto.MaxScore.HasValue)
            return BadRequest(new { message = "MaxScore is required for score grading type." });

        task.Name = dto.Name;
        task.GradingType = dto.GradingType;
        task.MaxScore = dto.GradingType == 2 ? dto.MaxScore : null;
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(MapToDto(task));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(int disciplineId, int id)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.DisciplineId == disciplineId);

        if (task == null)
            return NotFound(new { message = $"Task with id {id} not found." });

        _context.Tasks.Remove(task);

        var tasksToReorder = await _context.Tasks
            .Where(t => t.DisciplineId == disciplineId && t.Number > task.Number)
            .OrderBy(t => t.Number)
            .ToListAsync();

        foreach (var t in tasksToReorder)
            t.Number--;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id}/priority")]
    public async Task<IActionResult> ChangePriority(int disciplineId, int id, [FromQuery] string direction)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.DisciplineId == disciplineId);

        if (task == null)
            return NotFound(new { message = $"Task with id {id} not found." });

        DisciplineTask? neighbor = null;

        if (direction == "up" && task.Number > 1)
        {
            neighbor = await _context.Tasks
                .FirstOrDefaultAsync(t => t.DisciplineId == disciplineId && t.Number == task.Number - 1);
        }
        else if (direction == "down")
        {
            neighbor = await _context.Tasks
                .FirstOrDefaultAsync(t => t.DisciplineId == disciplineId && t.Number == task.Number + 1);
        }

        if (neighbor == null)
            return BadRequest(new { message = "Cannot change priority in this direction." });

        (task.Number, neighbor.Number) = (neighbor.Number, task.Number);
        task.UpdatedAt = DateTime.UtcNow;
        neighbor.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static DisciplineTaskDto MapToDto(DisciplineTask t) => new()
    {
        Id = t.Id,
        DisciplineId = t.DisciplineId,
        Number = t.Number,
        Name = t.Name,
        GradingType = t.GradingType,
        MaxScore = t.MaxScore,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt
    };
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeachAssist.Api.DTOs;
using TeachAssist.Domain.Data;
using TeachAssist.Domain.Models;

namespace TeachAssist.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireManager")]
public class GroupsController : ControllerBase
{
    private readonly DomainDbContext _context;

    public GroupsController(DomainDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<GroupDto>>> GetGroups()
    {
        var groups = await _context.Groups
            .OrderBy(g => g.YearStarted)
            .ThenBy(g => g.Name)
            .Select(g => MapToDto(g))
            .ToListAsync();
        return Ok(groups);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GroupDto>> GetGroup(int id)
    {
        var group = await _context.Groups.FindAsync(id);
        if (group == null)
            return NotFound(new { message = $"Group with id {id} not found." });
        return Ok(MapToDto(group));
    }

    [HttpPost]
    public async Task<ActionResult<GroupDto>> CreateGroup([FromBody] CreateGroupDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var uniquenessError = await ValidateGroupNameUniqueness(dto.Name, null);
        if (uniquenessError != null)
            return uniquenessError;

        var group = new DomainGroup
        {
            Name = dto.Name,
            ShortName = dto.ShortName,
            YearStarted = dto.YearStarted
        };

        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetGroup), new { id = group.Id }, MapToDto(group));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<GroupDto>> UpdateGroup(int id, [FromBody] UpdateGroupDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var group = await _context.Groups.FindAsync(id);
        if (group == null)
            return NotFound(new { message = $"Group with id {id} not found." });

        var uniquenessError = await ValidateGroupNameUniqueness(dto.Name, id);
        if (uniquenessError != null)
            return uniquenessError;

        group.Name = dto.Name;
        group.ShortName = dto.ShortName;
        group.YearStarted = dto.YearStarted;
        group.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(MapToDto(group));
    }

    private async Task<ActionResult<GroupDto>?> ValidateGroupNameUniqueness(string name, int? excludeId)
    {
        var exists = excludeId.HasValue
            ? await _context.Groups.AnyAsync(g => g.Name == name && g.Id != excludeId.Value)
            : await _context.Groups.AnyAsync(g => g.Name == name);

        if (exists)
            return BadRequest(new { message = "Group with this name already exists." });

        return null;
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGroup(int id)
    {
        var group = await _context.Groups.FindAsync(id);
        if (group == null)
            return NotFound(new { message = $"Group with id {id} not found." });

        _context.Groups.Remove(group);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static GroupDto MapToDto(DomainGroup g) => new()
    {
        Id = g.Id,
        Name = g.Name,
        ShortName = g.ShortName,
        YearStarted = g.YearStarted,
        CreatedAt = g.CreatedAt,
        UpdatedAt = g.UpdatedAt
    };
}

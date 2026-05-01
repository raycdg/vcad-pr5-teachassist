using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeachAssist.Api.DTOs;
using TeachAssist.Domain.Data;
using TeachAssist.Domain.Models;

namespace TeachAssist.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize(Policy = "RequireManager")]
public class StudentsController : ControllerBase
{
    private readonly DomainDbContext _context;

    public StudentsController(DomainDbContext context)
    {
        _context = context;
    }

    [HttpGet("groups/{groupId}/students")]
    public async Task<ActionResult<IEnumerable<StudentDto>>> GetStudentsByGroup(int groupId)
    {
        var groupExists = await _context.Groups.AnyAsync(g => g.Id == groupId);
        if (!groupExists)
            return NotFound(new { message = $"Group with id {groupId} not found." });

        var students = await _context.Students
            .Where(s => s.GroupId == groupId)
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .Select(s => MapToDto(s))
            .ToListAsync();
        return Ok(students);
    }

    [HttpGet("students/{id}")]
    public async Task<ActionResult<StudentDto>> GetStudent(int id)
    {
        var student = await _context.Students.FindAsync(id);
        if (student == null)
            return NotFound(new { message = $"Student with id {id} not found." });
        return Ok(MapToDto(student));
    }

    [HttpPost("students")]
    public async Task<ActionResult<StudentDto>> CreateStudent([FromBody] CreateStudentDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var groupExists = await _context.Groups.AnyAsync(g => g.Id == dto.GroupId);
        if (!groupExists)
            return BadRequest(new { message = $"Group with id {dto.GroupId} not found." });

        var student = new Student
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            GroupId = dto.GroupId
        };

        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetStudent), new { id = student.Id }, MapToDto(student));
    }

    [HttpPut("students/{id}")]
    public async Task<ActionResult<StudentDto>> UpdateStudent(int id, [FromBody] UpdateStudentDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var student = await _context.Students.FindAsync(id);
        if (student == null)
            return NotFound(new { message = $"Student with id {id} not found." });

        student.FirstName = dto.FirstName;
        student.LastName = dto.LastName;
        student.Email = dto.Email;
        student.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(MapToDto(student));
    }

    [HttpDelete("students/{id}")]
    public async Task<IActionResult> DeleteStudent(int id)
    {
        var student = await _context.Students.FindAsync(id);
        if (student == null)
            return NotFound(new { message = $"Student with id {id} not found." });

        _context.Students.Remove(student);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static StudentDto MapToDto(Student s) => new()
    {
        Id = s.Id,
        FirstName = s.FirstName,
        LastName = s.LastName,
        Email = s.Email,
        GroupId = s.GroupId,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt
    };
}

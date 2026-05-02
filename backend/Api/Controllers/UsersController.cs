using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeachAssist.Api.Data;
using TeachAssist.Api.DTOs;
using TeachAssist.Api.Models;

namespace TeachAssist.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly AuthDbContext _context;

    public UsersController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, AuthDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    [HttpGet]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> GetUsers([FromQuery] bool includeDeleted = false)
    {
        IQueryable<AppUser> query = includeDeleted
            ? _context.Users.IgnoreQueryFilters()
            : _context.Users;

        var users = await query.ToListAsync();
        var result = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? string.Empty,
                IsDeleted = user.IsDeleted,
                CreatedAt = user.CreatedAt,
            });
        }

        return Ok(result);
    }

    [HttpGet("teachers")]
    [Authorize(Policy = "RequireManager")]
    public async Task<IActionResult> GetTeachers()
    {
        var users = await _userManager.GetUsersInRoleAsync("Teacher");
        var result = users
            .Where(u => !u.IsDeleted)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email ?? string.Empty,
                Role = "Teacher",
                IsDeleted = u.IsDeleted,
                CreatedAt = u.CreatedAt,
            })
            .ToList();
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> GetUser(string id)
    {
        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Role = roles.FirstOrDefault() ?? string.Empty,
            IsDeleted = user.IsDeleted,
            CreatedAt = user.CreatedAt,
        });
    }

    [HttpPost]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            return BadRequest(new { message = "Email is required" });
        }

        if (string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new { message = "Password is required" });
        }

        if (string.IsNullOrWhiteSpace(dto.Role))
        {
            return BadRequest(new { message = "Role is required" });
        }

        var existingUser = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (existingUser != null)
        {
            return BadRequest(new { message = "User with this email already exists" });
        }

        if (!await _roleManager.RoleExistsAsync(dto.Role))
        {
            return BadRequest(new { message = $"Role '{dto.Role}' does not exist" });
        }

        var user = new AppUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(new { message = errors });
        }

        await _userManager.AddToRoleAsync(user, dto.Role);

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            Role = dto.Role,
            IsDeleted = false,
            CreatedAt = user.CreatedAt,
        });
    }

    [HttpPut("{id}/role")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> UpdateUserRole(string id, [FromBody] UpdateUserRoleDto dto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        if (!await _roleManager.RoleExistsAsync(dto.Role))
        {
            return BadRequest(new { message = $"Role '{dto.Role}' does not exist" });
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Any())
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
        }

        await _userManager.AddToRoleAsync(user, dto.Role);

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            Role = dto.Role,
            IsDeleted = user.IsDeleted,
            CreatedAt = user.CreatedAt,
        });
    }

    [HttpPut("{id}/reset-password")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> ResetPassword(string id, [FromBody] ResetPasswordDto dto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(new { message = errors });
        }

        return Ok(new { message = "Password reset successfully" });
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        user.IsDeleted = true;
        user.UpdatedAt = DateTime.UtcNow;
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(new { message = errors });
        }

        return NoContent();
    }

    [HttpPost("{id}/restore")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> RestoreUser(string id)
    {
        var user = await _userManager.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        if (!user.IsDeleted)
        {
            return BadRequest(new { message = "User is not deleted" });
        }

        user.IsDeleted = false;
        user.UpdatedAt = DateTime.UtcNow;
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(new { message = errors });
        }

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? string.Empty,
            IsDeleted = false,
            CreatedAt = user.CreatedAt,
        });
    }
}

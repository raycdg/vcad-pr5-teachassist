using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TeachAssist.Api.DTOs;
using TeachAssist.Api.Models;

namespace TeachAssist.Api.Controllers;

[ApiController]
[Route("api/account")]
[Authorize(Policy = "RequireTeacher")]
public class AccountController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;

    public AccountController(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User?.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "User not found" });

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.IsDeleted)
            return Unauthorized(new { message = "User not found" });

        var roles = await _userManager.GetRolesAsync(user);

        var profile = new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email!,
            UserName = user.UserName!,
            Roles = roles
        };

        return Ok(profile);
    }

    [HttpPut("email")]
    public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = User?.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "User not found" });

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.IsDeleted)
            return Unauthorized(new { message = "User not found" });

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
            return BadRequest(new { message = "Invalid password" });

        var newEmail = request.NewEmail.Trim().ToLower();
        var existingUser = await _userManager.FindByEmailAsync(newEmail);
        if (existingUser != null && existingUser.Id != user.Id)
            return BadRequest(new { message = "Email already in use" });

        user.Email = newEmail;
        user.UserName = newEmail;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(new { message = errors });
        }

        return Ok(new { message = "Email updated successfully" });
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = User?.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "User not found" });

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.IsDeleted)
            return Unauthorized(new { message = "User not found" });

        var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(new { message = errors });
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return Ok(new { message = "Password changed successfully" });
    }
}

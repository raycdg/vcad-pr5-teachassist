using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeachAssist.Api.Controllers;
using TeachAssist.Api.Data;
using TeachAssist.Api.DTOs;
using TeachAssist.Api.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace TeachAssist.Api.Tests;

public class AccountControllerTests
{
    private static AuthDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AuthDbContext(options);
    }

    private static UserManager<AppUser> CreateUserManager(AuthDbContext context)
    {
        var store = new UserStore<AppUser>(context);
        var loggerFactory = new LoggerFactory();
        return new UserManager<AppUser>(
            store,
            null!,
            new PasswordHasher<AppUser>(),
            Array.Empty<IUserValidator<AppUser>>(),
            Array.Empty<IPasswordValidator<AppUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            loggerFactory.CreateLogger<UserManager<AppUser>>()
        );
    }

    private static RoleManager<IdentityRole> CreateRoleManager(AuthDbContext context)
    {
        var roleStore = new RoleStore<IdentityRole>(context);
        return new RoleManager<IdentityRole>(
            roleStore,
            Array.Empty<IRoleValidator<IdentityRole>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!
        );
    }

    private static async Task SeedRoles(AuthDbContext context)
    {
        var roleManager = CreateRoleManager(context);
        var roleNames = new[] { "Admin", "Manager", "Teacher" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }

    private static AccountController CreateController(AuthDbContext context, string? userId = null)
    {
        var userManager = CreateUserManager(context);
        var controller = new AccountController(userManager);

        if (!string.IsNullOrEmpty(userId))
        {
            var claims = new List<Claim> { new Claim("userId", userId) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        return controller;
    }

    #region GetProfile Tests

    [Fact]
    public async Task GetProfile_ReturnsUnauthorized_WhenUserIdClaimMissing()
    {
        await using var context = CreateInMemoryContext(nameof(GetProfile_ReturnsUnauthorized_WhenUserIdClaimMissing));
        var controller = CreateController(context);

        var result = await controller.GetProfile();

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetProfile_ReturnsUnauthorized_WhenUserNotFound()
    {
        await using var context = CreateInMemoryContext(nameof(GetProfile_ReturnsUnauthorized_WhenUserNotFound));
        var controller = CreateController(context, "nonexistent-id");

        var result = await controller.GetProfile();

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetProfile_ReturnsUnauthorized_WhenUserIsDeleted()
    {
        await using var context = CreateInMemoryContext(nameof(GetProfile_ReturnsUnauthorized_WhenUserIsDeleted));
        var userManager = CreateUserManager(context);
        var user = new AppUser { UserName = "deleted@test.local", Email = "deleted@test.local", IsDeleted = true };
        await userManager.CreateAsync(user, "password");

        var controller = CreateController(context, user.Id);

        var result = await controller.GetProfile();

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetProfile_ReturnsOk_WithCorrectProfile()
    {
        await using var context = CreateInMemoryContext(nameof(GetProfile_ReturnsOk_WithCorrectProfile));
        await SeedRoles(context);
        var userManager = CreateUserManager(context);
        var user = new AppUser { UserName = "test@test.local", Email = "test@test.local" };
        await userManager.CreateAsync(user, "password");
        await userManager.AddToRoleAsync(user, "Teacher");

        var controller = CreateController(context, user.Id);

        var result = await controller.GetProfile();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var profile = Assert.IsType<UserProfileDto>(okResult.Value);
        Assert.Equal("test@test.local", profile.Email);
        Assert.Contains("Teacher", profile.Roles);
    }

    #endregion

    #region ChangeEmail Tests

    [Fact]
    public async Task ChangeEmail_ReturnsBadRequest_WhenModelInvalid()
    {
        await using var context = CreateInMemoryContext(nameof(ChangeEmail_ReturnsBadRequest_WhenModelInvalid));
        var controller = CreateController(context, "some-id");

        controller.ModelState.AddModelError("NewEmail", "Required");
        var request = new ChangeEmailRequest { NewEmail = "", Password = "" };

        var result = await controller.ChangeEmail(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ChangeEmail_ReturnsUnauthorized_WhenUserIdClaimMissing()
    {
        await using var context = CreateInMemoryContext(nameof(ChangeEmail_ReturnsUnauthorized_WhenUserIdClaimMissing));
        var controller = CreateController(context);

        var request = new ChangeEmailRequest { NewEmail = "new@test.local", Password = "password" };

        var result = await controller.ChangeEmail(request);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task ChangeEmail_ReturnsBadRequest_WhenPasswordInvalid()
    {
        await using var context = CreateInMemoryContext(nameof(ChangeEmail_ReturnsBadRequest_WhenPasswordInvalid));
        var userManager = CreateUserManager(context);
        var user = new AppUser { UserName = "test@test.local", Email = "test@test.local" };
        await userManager.CreateAsync(user, "correctpassword");

        var controller = CreateController(context, user.Id);
        var request = new ChangeEmailRequest { NewEmail = "new@test.local", Password = "wrongpassword" };

        var result = await controller.ChangeEmail(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Invalid password", badRequest.Value?.ToString() ?? "");
    }

    [Fact]
    public async Task ChangeEmail_ReturnsBadRequest_WhenNewEmailAlreadyInUse()
    {
        await using var context = CreateInMemoryContext(nameof(ChangeEmail_ReturnsBadRequest_WhenNewEmailAlreadyInUse));
        var userManager = CreateUserManager(context);
        var user1 = new AppUser { UserName = "user1@test.local", Email = "user1@test.local" };
        var user2 = new AppUser { UserName = "user2@test.local", Email = "user2@test.local" };
        await userManager.CreateAsync(user1, "password");
        await userManager.CreateAsync(user2, "password");

        var controller = CreateController(context, user1.Id);
        var request = new ChangeEmailRequest { NewEmail = "user2@test.local", Password = "password" };

        var result = await controller.ChangeEmail(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Email already in use", badRequest.Value?.ToString() ?? "");
    }

    [Fact]
    public async Task ChangeEmail_ReturnsOk_WhenEmailChangedSuccessfully()
    {
        await using var context = CreateInMemoryContext(nameof(ChangeEmail_ReturnsOk_WhenEmailChangedSuccessfully));
        var userManager = CreateUserManager(context);
        var user = new AppUser { UserName = "test@test.local", Email = "test@test.local" };
        await userManager.CreateAsync(user, "password");

        var controller = CreateController(context, user.Id);
        var request = new ChangeEmailRequest { NewEmail = "new@test.local", Password = "password" };

        var result = await controller.ChangeEmail(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("Email updated successfully", okResult.Value?.ToString() ?? "");

        var updatedUser = await userManager.FindByIdAsync(user.Id);
        Assert.Equal("new@test.local", updatedUser?.Email);
        Assert.Equal("new@test.local", updatedUser?.UserName);
    }

    #endregion

    #region ChangePassword Tests

    [Fact]
    public async Task ChangePassword_ReturnsBadRequest_WhenModelInvalid()
    {
        await using var context = CreateInMemoryContext(nameof(ChangePassword_ReturnsBadRequest_WhenModelInvalid));
        var controller = CreateController(context, "some-id");

        controller.ModelState.AddModelError("OldPassword", "Required");
        var request = new ChangePasswordRequest { OldPassword = "", NewPassword = "" };

        var result = await controller.ChangePassword(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ChangePassword_ReturnsUnauthorized_WhenUserIdClaimMissing()
    {
        await using var context = CreateInMemoryContext(nameof(ChangePassword_ReturnsUnauthorized_WhenUserIdClaimMissing));
        var controller = CreateController(context);

        var request = new ChangePasswordRequest { OldPassword = "old", NewPassword = "new" };

        var result = await controller.ChangePassword(request);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task ChangePassword_ReturnsBadRequest_WhenOldPasswordInvalid()
    {
        await using var context = CreateInMemoryContext(nameof(ChangePassword_ReturnsBadRequest_WhenOldPasswordInvalid));
        var userManager = CreateUserManager(context);
        var user = new AppUser { UserName = "test@test.local", Email = "test@test.local" };
        await userManager.CreateAsync(user, "correctpassword");

        var controller = CreateController(context, user.Id);
        var request = new ChangePasswordRequest { OldPassword = "wrongpassword", NewPassword = "newpassword123" };

        var result = await controller.ChangePassword(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ChangePassword_ReturnsOk_WhenPasswordChangedSuccessfully()
    {
        await using var context = CreateInMemoryContext(nameof(ChangePassword_ReturnsOk_WhenPasswordChangedSuccessfully));
        var userManager = CreateUserManager(context);
        var user = new AppUser { UserName = "test@test.local", Email = "test@test.local" };
        await userManager.CreateAsync(user, "oldpassword");

        var controller = CreateController(context, user.Id);
        var request = new ChangePasswordRequest { OldPassword = "oldpassword", NewPassword = "newpassword123" };

        var result = await controller.ChangePassword(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("Password changed successfully", okResult.Value?.ToString() ?? "");

        var passwordValid = await userManager.CheckPasswordAsync(user, "newpassword123");
        Assert.True(passwordValid);
    }

    #endregion
}

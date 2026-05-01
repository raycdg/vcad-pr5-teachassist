using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeachAssist.Api.Controllers;
using TeachAssist.Api.Data;
using TeachAssist.Api.DTOs;
using TeachAssist.Api.Models;

namespace TeachAssist.Api.Tests;

public class UsersControllerTests
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
        var store = new RoleStore<IdentityRole>(context);
        return new RoleManager<IdentityRole>(
            store,
            Array.Empty<IRoleValidator<IdentityRole>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!
        );
    }

    private static UsersController CreateController(AuthDbContext context, string? role = null)
    {
        var userManager = CreateUserManager(context);
        var roleManager = CreateRoleManager(context);

        var controller = new UsersController(userManager, roleManager, context);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Role, role ?? "User"),
                })),
            },
        };

        return controller;
    }

    [Fact]
    public async Task GetUsers_ReturnsEmptyList_WhenNoUsersExist()
    {
        await using var context = CreateInMemoryContext(nameof(GetUsers_ReturnsEmptyList_WhenNoUsersExist));
        await CreateRoleAsync(context, "Admin");
        var controller = CreateController(context, "Admin");

        var result = await controller.GetUsers();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var users = Assert.IsType<List<UserDto>>(okResult.Value);
        Assert.Empty(users);
    }

    [Fact]
    public async Task GetUsers_ReturnsNonDeletedUsers_ByDefault()
    {
        await using var context = CreateInMemoryContext(nameof(GetUsers_ReturnsNonDeletedUsers_ByDefault));
        await CreateRoleAsync(context, "Admin");
        var userManager = CreateUserManager(context);
        var user = new AppUser { UserName = "user@test.local", Email = "user@test.local" };
        await userManager.CreateAsync(user, "pass123");

        var deletedUser = new AppUser { UserName = "deleted@test.local", Email = "deleted@test.local", IsDeleted = true };
        await userManager.CreateAsync(deletedUser, "pass123");

        var controller = CreateController(context, "Admin");

        var result = await controller.GetUsers();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var users = Assert.IsType<List<UserDto>>(okResult.Value);
        Assert.Single(users);
        Assert.Equal("user@test.local", users[0].Email);
    }

    [Fact]
    public async Task GetUsers_ReturnsAllUsers_WhenIncludeDeletedIsTrue()
    {
        await using var context = CreateInMemoryContext(nameof(GetUsers_ReturnsAllUsers_WhenIncludeDeletedIsTrue));
        await CreateRoleAsync(context, "Admin");
        var userManager = CreateUserManager(context);
        var user = new AppUser { UserName = "user@test.local", Email = "user@test.local" };
        await userManager.CreateAsync(user, "pass123");

        var deletedUser = new AppUser { UserName = "deleted@test.local", Email = "deleted@test.local", IsDeleted = true };
        await userManager.CreateAsync(deletedUser, "pass123");

        var controller = CreateController(context, "Admin");

        var result = await controller.GetUsers(includeDeleted: true);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var users = Assert.IsType<List<UserDto>>(okResult.Value);
        Assert.Equal(2, users.Count);
    }

    [Fact]
    public async Task CreateUser_ReturnsCreatedUser_WithValidData()
    {
        await using var context = CreateInMemoryContext(nameof(CreateUser_ReturnsCreatedUser_WithValidData));
        await CreateRoleAsync(context, "Admin");
        await CreateRoleAsync(context, "Teacher");
        var controller = CreateController(context, "Admin");

        var dto = new CreateUserDto { Email = "teacher@test.local", Password = "pass123", Role = "Teacher" };

        var result = await controller.CreateUser(dto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var user = Assert.IsType<UserDto>(okResult.Value);
        Assert.Equal("teacher@test.local", user.Email);
        Assert.Equal("Teacher", user.Role);
    }

    [Fact]
    public async Task DeleteUser_SoftDeletesUser()
    {
        await using var context = CreateInMemoryContext(nameof(DeleteUser_SoftDeletesUser));
        await CreateRoleAsync(context, "Admin");
        var userManager = CreateUserManager(context);
        var user = new AppUser { UserName = "user@test.local", Email = "user@test.local" };
        await userManager.CreateAsync(user, "pass123");

        var controller = CreateController(context, "Admin");

        var result = await controller.DeleteUser(user.Id);

        Assert.IsType<NoContentResult>(result);
        var deletedUser = await userManager.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == user.Id);
        Assert.True(deletedUser.IsDeleted);
    }

    [Fact]
    public async Task RestoreUser_RestoresDeletedUser()
    {
        await using var context = CreateInMemoryContext(nameof(RestoreUser_RestoresDeletedUser));
        await CreateRoleAsync(context, "Admin");
        var userManager = CreateUserManager(context);
        var user = new AppUser { UserName = "user@test.local", Email = "user@test.local", IsDeleted = true };
        await userManager.CreateAsync(user, "pass123");

        var controller = CreateController(context, "Admin");

        var result = await controller.RestoreUser(user.Id);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var restored = Assert.IsType<UserDto>(okResult.Value);
        Assert.False(restored.IsDeleted);
    }

    [Fact]
    public async Task ResetPassword_ReturnsNotFound_WhenUserDoesNotExist()
    {
        await using var context = CreateInMemoryContext(nameof(ResetPassword_ReturnsNotFound_WhenUserDoesNotExist));
        await CreateRoleAsync(context, "Admin");
        var controller = CreateController(context, "Admin");

        var result = await controller.ResetPassword("nonexistent-id", new ResetPasswordDto { NewPassword = "newpass123" });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateUserRole_ChangesRoleSuccessfully()
    {
        await using var context = CreateInMemoryContext(nameof(UpdateUserRole_ChangesRoleSuccessfully));
        await CreateRoleAsync(context, "Admin");
        await CreateRoleAsync(context, "Teacher");
        await CreateRoleAsync(context, "Manager");
        var userManager = CreateUserManager(context);
        var user = new AppUser { UserName = "user@test.local", Email = "user@test.local" };
        await userManager.CreateAsync(user, "pass123");
        await userManager.AddToRoleAsync(user, "Teacher");

        var controller = CreateController(context, "Admin");

        var result = await controller.UpdateUserRole(user.Id, new UpdateUserRoleDto { Role = "Manager" });

        var okResult = Assert.IsType<OkObjectResult>(result);
        var updated = Assert.IsType<UserDto>(okResult.Value);
        Assert.Equal("Manager", updated.Role);
    }

    private static async Task CreateRoleAsync(AuthDbContext context, string roleName)
    {
        var roleManager = CreateRoleManager(context);
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
}

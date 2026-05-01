using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TeachAssist.Api.Controllers;
using TeachAssist.Api.Data;
using TeachAssist.Api.DTOs;
using TeachAssist.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace TeachAssist.Api.Tests;

public class AuthControllerTests
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

    private static AuthController CreateController(AuthDbContext context)
    {
        var userManager = CreateUserManager(context);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:SecretKey"] = "TestSecretKeyForJwtTokenGenerationMustBeLongEnough!",
                ["Jwt:ExpiryMinutes"] = "60",
            })
            .Build();

        return new AuthController(userManager, configuration);
    }

    [Fact]
    public async Task Login_ReturnsToken_WhenCredentialsAreValid()
    {
        await using var context = CreateInMemoryContext(nameof(Login_ReturnsToken_WhenCredentialsAreValid));
        var userManager = CreateUserManager(context);
        var user = new AppUser { UserName = "admin@test.local", Email = "admin@test.local" };
        await userManager.CreateAsync(user, "admin");

        var controller = CreateController(context);
        var request = new LoginRequest { Email = "admin@test.local", Password = "admin" };

        var result = await controller.Login(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<LoginResponse>(okResult.Value);
        Assert.Equal("admin@test.local", response.Email);
        Assert.NotEmpty(response.Token);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenPasswordIsWrong()
    {
        await using var context = CreateInMemoryContext(nameof(Login_ReturnsUnauthorized_WhenPasswordIsWrong));
        var userManager = CreateUserManager(context);
        var user = new AppUser { UserName = "user@test.local", Email = "user@test.local" };
        await userManager.CreateAsync(user, "correctpassword");

        var controller = CreateController(context);
        var request = new LoginRequest { Email = "user@test.local", Password = "wrongpassword" };

        var result = await controller.Login(request);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenEmailDoesNotExist()
    {
        await using var context = CreateInMemoryContext(nameof(Login_ReturnsUnauthorized_WhenEmailDoesNotExist));
        var controller = CreateController(context);
        var request = new LoginRequest { Email = "nonexistent@test.local", Password = "anypassword" };

        var result = await controller.Login(request);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenUserIsDeleted()
    {
        await using var context = CreateInMemoryContext(nameof(Login_ReturnsUnauthorized_WhenUserIsDeleted));
        var userManager = CreateUserManager(context);
        var user = new AppUser { UserName = "deleted@test.local", Email = "deleted@test.local", IsDeleted = true };
        await userManager.CreateAsync(user, "password");

        var controller = CreateController(context);
        var request = new LoginRequest { Email = "deleted@test.local", Password = "password" };

        var result = await controller.Login(request);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}

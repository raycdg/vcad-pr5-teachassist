using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TeachAssist.Api.Data;
using TeachAssist.Api.Models;
using TeachAssist.Domain.Data;

namespace TeachAssist.Api.Tests;

public static class TestHelper
{
    public static AuthDbContext CreateAuthContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AuthDbContext(options);
    }

    public static UserManager<AppUser> CreateUserManager(AuthDbContext context)
    {
        var store = new UserStore<AppUser>(context);
        return new UserManager<AppUser>(
            store,
            null!,
            new PasswordHasher<AppUser>(),
            Array.Empty<IUserValidator<AppUser>>(),
            Array.Empty<IPasswordValidator<AppUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            null!
        );
    }

    public static DomainDbContext CreateDomainContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<DomainDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new DomainDbContext(options);
    }
}

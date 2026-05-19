using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TeachAssist.Api.Authorization;
using TeachAssist.Api.Data;
using TeachAssist.Api.Logging;
using TeachAssist.Api.Middleware;
using TeachAssist.Api.Models;
using TeachAssist.Api.Options;
using TeachAssist.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod()
              .WithExposedHeaders("Content-Disposition");
    });
});
builder.Services.AddOpenApi();

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
});
// Remove HttpContextAccessor registration if present

var smtpOptions = builder.Configuration.GetSection("Smtp").Get<SmtpOptions>() ?? new SmtpOptions();
builder.Services.AddSingleton(smtpOptions);

var logPath = Path.Combine(builder.Environment.ContentRootPath, "grade-notifications.log");
builder.Logging.AddProvider(new FileLoggerProvider(logPath));
builder.Logging.AddFilter<FileLoggerProvider>((category, level) =>
    category?.Contains("GradeNotification", StringComparison.Ordinal) == true
    && level >= LogLevel.Information);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5433;Database=teachassist_3_1;Username=postgres;Password=postgres";

builder.Services.AddDbContext<TeachAssist.Domain.Data.DomainDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly("TeachAssist.Api")));

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 4;
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddDefaultTokenProviders();

var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireManager", policy => policy.RequireRole("Manager", "Admin"));
    options.AddPolicy("RequireTeacher", policy => policy.RequireRole("Teacher", "Manager", "Admin"));
});

builder.Services.AddScoped<IAuthorizationHandler, TeachAssist.Api.Authorization.ResourceOwnerAuthorizationHandler>();

builder.Services.AddScoped<GradeNotificationAdapter>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseRateLimiter();
app.UseCors();
app.UseHttpsRedirection();
app.MapOpenApi();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var authContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

    var roleNames = new[] { "Admin", "Manager", "Teacher" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    var adminEmail = "admin@teachassis.local";
    var adminPassword = "admin";

    var existingAdmin = await userManager.Users
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(u => u.Email == adminEmail);

    if (existingAdmin == null)
    {
        var admin = new AppUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        var result = await userManager.CreateAsync(admin, adminPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to seed admin user: {errors}");
        }
        await userManager.AddToRoleAsync(admin, "Admin");
    }
    else if (existingAdmin.IsDeleted)
    {
        existingAdmin.IsDeleted = false;
        existingAdmin.UpdatedAt = DateTime.UtcNow;
        var updateResult = await userManager.UpdateAsync(existingAdmin);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to restore admin user: {errors}");
        }

        var passwordHash = await userManager.HasPasswordAsync(existingAdmin);
        if (!passwordHash)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(existingAdmin);
            var resetResult = await userManager.ResetPasswordAsync(existingAdmin, token, adminPassword);
            if (!resetResult.Succeeded)
            {
                var errors = string.Join(", ", resetResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to reset admin password: {errors}");
            }
        }

        var roles = await userManager.GetRolesAsync(existingAdmin);
        if (!roles.Contains("Admin"))
        {
            await userManager.AddToRoleAsync(existingAdmin, "Admin");
        }
    }
    else
    {
        var roles = await userManager.GetRolesAsync(existingAdmin);
        if (!roles.Contains("Admin"))
        {
            await userManager.AddToRoleAsync(existingAdmin, "Admin");
        }
    }
}

app.Run();

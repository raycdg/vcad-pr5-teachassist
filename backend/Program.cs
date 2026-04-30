using Microsoft.EntityFrameworkCore;
using TeachAssist.Api.Logging;
using TeachAssist.Api.Options;
using TeachAssist.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

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
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<GradeNotificationAdapter>();

var app = builder.Build();

app.MapOpenApi();
app.MapControllers();

app.Run();

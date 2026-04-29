using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5433;Database=teachassist_3_1;Username=postgres;Password=postgres";

builder.Services.AddDbContext<TeachAssist.Domain.Data.DomainDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

app.MapOpenApi();
app.MapControllers();

app.Run();

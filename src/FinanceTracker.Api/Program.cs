using FinanceTracker.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

builder.Services.AddDbContext<FinanceTrackerDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddHealthChecks().AddDbContextCheck<FinanceTrackerDbContext>();

var app = builder.Build();

if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<FinanceTrackerDbContext>();
    await db.Database.MigrateAsync();
}

app.UseHttpsRedirection();

app.MapGet("/", () => Results.Ok(new
{
    service = "FinanceTracker.Api",
    status = "running"
}));

app.MapHealthChecks("/health");

app.Run();

public partial class Program;

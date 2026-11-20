var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/", () => Results.Ok(new
{
    service = "FinanceTracker.Api",
    status = "running"
}));

app.MapHealthChecks("/health");

app.Run();

public partial class Program;

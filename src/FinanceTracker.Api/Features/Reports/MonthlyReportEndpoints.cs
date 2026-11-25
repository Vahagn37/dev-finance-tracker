using System.Security.Claims;

namespace FinanceTracker.Api.Features.Reports;

public static class MonthlyReportEndpoints
{
    public static IEndpointRouteBuilder MapMonthlyReportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/reports/monthly", async (
            int year,
            int month,
            ClaimsPrincipal principal,
            MonthlyReportService service,
            CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Results.Unauthorized();
            }

            if (year is < 2000 or > 9999 || month is < 1 or > 12)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["period"] = ["Year must be between 2000 and 9999, and month must be between 1 and 12."]
                });
            }

            return Results.Ok(await service.GetAsync(userId, year, month, cancellationToken));
        })
        .WithTags("Reports")
        .RequireAuthorization();

        return endpoints;
    }
}

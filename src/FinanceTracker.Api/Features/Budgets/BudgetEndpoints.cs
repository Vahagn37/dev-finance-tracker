using System.Security.Claims;

namespace FinanceTracker.Api.Features.Budgets;

public static class BudgetEndpoints
{
    public static IEndpointRouteBuilder MapBudgetEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/budgets")
            .WithTags("Budgets")
            .RequireAuthorization();

        group.MapGet("", async (
            int year,
            int month,
            ClaimsPrincipal principal,
            BudgetService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
            {
                return Results.Unauthorized();
            }

            if (!IsValidMonth(year, month))
            {
                return InvalidPeriod();
            }

            return Results.Ok(await service.ListAsync(userId, year, month, cancellationToken));
        });

        group.MapPut("/{categoryId:guid}", async (
            Guid categoryId,
            int year,
            int month,
            UpsertBudgetRequest request,
            ClaimsPrincipal principal,
            BudgetService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
            {
                return Results.Unauthorized();
            }

            if (!IsValidMonth(year, month))
            {
                return InvalidPeriod();
            }

            var result = await service.UpsertAsync(userId, categoryId, year, month, request, cancellationToken);
            if (result.Response is not null)
            {
                return Results.Ok(result.Response);
            }

            return result.Error switch
            {
                BudgetError.InvalidAmount => Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["amountLimit"] = ["AmountLimit must be greater than zero."]
                }),
                BudgetError.CategoryNotFound => Results.NotFound(),
                BudgetError.NotExpenseCategory => Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["categoryId"] = ["A budget can only target an Expense category."]
                }),
                BudgetError.Conflict => Results.Conflict(new { message = "The budget could not be saved because it conflicts with another update." }),
                _ => Results.Problem()
            };
        });

        return endpoints;
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    private static bool IsValidMonth(int year, int month) => year is >= 2000 and <= 9999 && month is >= 1 and <= 12;

    private static IResult InvalidPeriod() => Results.ValidationProblem(new Dictionary<string, string[]>
    {
        ["period"] = ["Year must be between 2000 and 9999, and month must be between 1 and 12."]
    });
}

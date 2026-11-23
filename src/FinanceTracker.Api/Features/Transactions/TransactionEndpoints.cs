using System.Security.Claims;
using FinanceTracker.Api.Domain;

namespace FinanceTracker.Api.Features.Transactions;

public static class TransactionEndpoints
{
    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/transactions")
            .WithTags("Transactions")
            .RequireAuthorization();

        group.MapGet("", async (
            DateTime? from,
            DateTime? to,
            Guid? categoryId,
            string? type,
            int? page,
            int? pageSize,
            ClaimsPrincipal principal,
            TransactionService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
            {
                return Results.Unauthorized();
            }

            if (!TryParseType(type, out var categoryType, out var typeError))
            {
                return typeError!;
            }

            var currentPage = page ?? 1;
            var currentPageSize = pageSize ?? 25;
            if (currentPage < 1 || currentPageSize is < 1 or > 100)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["pagination"] = ["Page must be at least 1 and pageSize must be between 1 and 100."]
                });
            }

            if (from.HasValue && to.HasValue && from.Value > to.Value)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["dateRange"] = ["The from date must be before or equal to the to date."]
                });
            }

            return Results.Ok(await service.ListAsync(
                userId,
                from,
                to,
                categoryId,
                categoryType,
                currentPage,
                currentPageSize,
                cancellationToken));
        });

        group.MapPost("", async (
            CreateTransactionRequest request,
            ClaimsPrincipal principal,
            TransactionService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
            {
                return Results.Unauthorized();
            }

            var result = await service.CreateAsync(userId, request, cancellationToken);
            return result.Response is not null
                ? Results.Created($"/api/transactions/{result.Response.Id}", result.Response)
                : ToErrorResult(result.Error!.Value);
        });

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateTransactionRequest request,
            ClaimsPrincipal principal,
            TransactionService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
            {
                return Results.Unauthorized();
            }

            var result = await service.UpdateAsync(userId, id, request, cancellationToken);
            return result.Response is not null
                ? Results.Ok(result.Response)
                : ToErrorResult(result.Error!.Value);
        });

        group.MapDelete("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal principal,
            TransactionService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
            {
                return Results.Unauthorized();
            }

            var error = await service.DeleteAsync(userId, id, cancellationToken);
            return error is null ? Results.NoContent() : ToErrorResult(error.Value);
        });

        return endpoints;
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    private static bool TryParseType(string? type, out CategoryType? categoryType, out IResult? error)
    {
        categoryType = null;
        error = null;
        if (type is null)
        {
            return true;
        }

        if (Enum.TryParse(type, true, out CategoryType parsedType))
        {
            categoryType = parsedType;
            return true;
        }

        error = Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["type"] = ["Type must be Income or Expense."]
        });
        return false;
    }

    private static IResult ToErrorResult(TransactionError error) => error switch
    {
        TransactionError.NotFound => Results.NotFound(),
        TransactionError.CategoryNotFound => Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["categoryId"] = ["The category does not exist or does not belong to the current user."]
        }),
        TransactionError.InvalidAmount => Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["amount"] = ["Amount must be greater than zero."]
        }),
        TransactionError.InvalidDate => Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["occurredAtUtc"] = ["A valid occurrence date is required."]
        }),
        TransactionError.InvalidDescription => Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["description"] = ["Description cannot exceed 500 characters."]
        }),
        _ => Results.Problem()
    };
}

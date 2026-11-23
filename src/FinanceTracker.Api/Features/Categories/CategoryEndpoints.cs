using System.Security.Claims;
using FinanceTracker.Api.Domain;

namespace FinanceTracker.Api.Features.Categories;

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/categories")
            .WithTags("Categories")
            .RequireAuthorization();

        group.MapGet("", async (
            string? type,
            ClaimsPrincipal principal,
            CategoryService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
            {
                return Results.Unauthorized();
            }

            CategoryType? categoryType = null;
            if (type is not null)
            {
                if (!Enum.TryParse(type, true, out CategoryType parsedType))
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["type"] = ["Type must be Income or Expense."]
                    });
                }

                categoryType = parsedType;
            }

            return Results.Ok(await service.ListAsync(userId, categoryType, cancellationToken));
        });

        group.MapPost("", async (
            CreateCategoryRequest request,
            ClaimsPrincipal principal,
            CategoryService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
            {
                return Results.Unauthorized();
            }

            var result = await service.CreateAsync(userId, request, cancellationToken);
            if (result.Response is not null)
            {
                return Results.Created($"/api/categories/{result.Response.Id}", result.Response);
            }

            return result.Duplicate
                ? Results.Conflict(new { message = "A category with this name and type already exists." })
                : InvalidRequest();
        });

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateCategoryRequest request,
            ClaimsPrincipal principal,
            CategoryService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
            {
                return Results.Unauthorized();
            }

            var result = await service.UpdateAsync(userId, id, request, cancellationToken);
            if (result.Response is not null)
            {
                return Results.Ok(result.Response);
            }

            return result.Duplicate
                ? Results.Conflict(new { message = "A category with this name and type already exists." })
                : string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 80
                    ? InvalidRequest()
                    : Results.NotFound();
        });

        group.MapDelete("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal principal,
            CategoryService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
            {
                return Results.Unauthorized();
            }

            return await service.DeleteAsync(userId, id, cancellationToken) switch
            {
                DeleteCategoryResult.Deleted => Results.NoContent(),
                DeleteCategoryResult.HasTransactions => Results.Conflict(new { message = "Categories with transactions cannot be deleted." }),
                DeleteCategoryResult.HasBudgets => Results.Conflict(new { message = "Categories with budgets cannot be deleted." }),
                _ => Results.NotFound()
            };
        });

        return endpoints;
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    private static IResult InvalidRequest() => Results.ValidationProblem(new Dictionary<string, string[]>
    {
        ["request"] = ["Name must contain between 1 and 80 characters. Type must be Income or Expense."]
    });
}

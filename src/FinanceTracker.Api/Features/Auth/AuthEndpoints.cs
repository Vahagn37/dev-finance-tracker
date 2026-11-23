using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace FinanceTracker.Api.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth").WithTags("Authentication");

        group.MapPost("/register", async (
            RegisterRequest request,
            AuthService auth,
            CancellationToken cancellationToken) =>
        {
            if (request.Password.Length < 8 || !request.Email.Contains('@'))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["request"] = ["A valid email and a password of at least 8 characters are required."]
                });
            }

            var result = await auth.RegisterAsync(request, cancellationToken);
            return result.Success
                ? Results.Created("/api/auth/me", result.Response)
                : Results.Conflict(new { message = "An account with this email already exists." });
        });

        group.MapPost("/login", async (
            LoginRequest request,
            AuthService auth,
            CancellationToken cancellationToken) =>
        {
            var response = await auth.LoginAsync(request, cancellationToken);
            return response is null
                ? Results.Unauthorized()
                : Results.Ok(response);
        });

        group.MapGet("/me", async (
            ClaimsPrincipal principal,
            AuthService auth,
            CancellationToken cancellationToken) =>
        {
            var idValue = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
            return !Guid.TryParse(idValue, out var userId)
                ? Results.Unauthorized()
                : await auth.GetUserAsync(userId, cancellationToken) is { } user
                    ? Results.Ok(new CurrentUserResponse(user.Id, user.Email, user.CreatedAtUtc))
                    : Results.NotFound();
        }).RequireAuthorization();

        return endpoints;
    }
}

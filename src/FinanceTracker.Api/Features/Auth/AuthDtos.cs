namespace FinanceTracker.Api.Features.Auth;

public sealed record RegisterRequest(string Email, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record AuthResponse(string AccessToken, DateTime ExpiresAtUtc);
public sealed record CurrentUserResponse(Guid Id, string Email, DateTime CreatedAtUtc);

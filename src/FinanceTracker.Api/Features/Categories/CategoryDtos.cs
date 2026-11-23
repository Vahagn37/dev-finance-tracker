namespace FinanceTracker.Api.Features.Categories;

public sealed record CreateCategoryRequest(string Name, string Type);
public sealed record UpdateCategoryRequest(string Name);
public sealed record CategoryResponse(Guid Id, string Name, string Type, DateTime CreatedAtUtc);

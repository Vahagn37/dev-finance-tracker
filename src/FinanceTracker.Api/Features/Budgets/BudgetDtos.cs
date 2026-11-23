namespace FinanceTracker.Api.Features.Budgets;

public sealed record UpsertBudgetRequest(decimal AmountLimit);
public sealed record BudgetResponse(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    int Year,
    int Month,
    decimal AmountLimit);

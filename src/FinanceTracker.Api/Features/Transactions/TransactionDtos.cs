namespace FinanceTracker.Api.Features.Transactions;

public sealed record CreateTransactionRequest(
    Guid CategoryId,
    decimal Amount,
    DateTime OccurredAtUtc,
    string? Description);

public sealed record UpdateTransactionRequest(
    Guid CategoryId,
    decimal Amount,
    DateTime OccurredAtUtc,
    string? Description);

public sealed record TransactionResponse(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string CategoryType,
    decimal Amount,
    DateTime OccurredAtUtc,
    string? Description,
    DateTime CreatedAtUtc);

public sealed record PagedTransactionsResponse(
    IReadOnlyList<TransactionResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

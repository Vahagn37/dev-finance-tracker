using FinanceTracker.Api.Domain;
using FinanceTracker.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Api.Features.Transactions;

public sealed class TransactionService(FinanceTrackerDbContext db)
{
    public async Task<PagedTransactionsResponse> ListAsync(
        Guid userId,
        DateTime? from,
        DateTime? to,
        Guid? categoryId,
        CategoryType? categoryType,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = db.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.UserId == userId);

        if (from.HasValue)
        {
            query = query.Where(transaction => transaction.OccurredAtUtc >= NormalizeUtc(from.Value));
        }

        if (to.HasValue)
        {
            query = query.Where(transaction => transaction.OccurredAtUtc <= NormalizeUtc(to.Value));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(transaction => transaction.CategoryId == categoryId.Value);
        }

        if (categoryType.HasValue)
        {
            query = query.Where(transaction => transaction.Category.Type == categoryType.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(transaction => transaction.OccurredAtUtc)
            .ThenByDescending(transaction => transaction.CreatedAtUtc)
            .ThenByDescending(transaction => transaction.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(transaction => new TransactionResponse(
                transaction.Id,
                transaction.CategoryId,
                transaction.Category.Name,
                transaction.Category.Type.ToString(),
                transaction.Amount,
                transaction.OccurredAtUtc,
                transaction.Description,
                transaction.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedTransactionsResponse(
            items,
            page,
            pageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize));
    }

    public async Task<(TransactionResponse? Response, TransactionError? Error)> CreateAsync(
        Guid userId,
        CreateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var category = await db.Categories.SingleOrDefaultAsync(
            candidate => candidate.Id == request.CategoryId && candidate.UserId == userId,
            cancellationToken);
        if (category is null)
        {
            return (null, TransactionError.CategoryNotFound);
        }

        var validationError = Validate(request.Amount, request.OccurredAtUtc, request.Description);
        if (validationError.HasValue)
        {
            return (null, validationError.Value);
        }

        var transaction = new FinanceTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CategoryId = category.Id,
            Amount = request.Amount,
            OccurredAtUtc = NormalizeUtc(request.OccurredAtUtc),
            Description = NormalizeDescription(request.Description),
            CreatedAtUtc = DateTime.UtcNow,
            Category = category
        };
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync(cancellationToken);
        return (ToResponse(transaction), null);
    }

    public async Task<(TransactionResponse? Response, TransactionError? Error)> UpdateAsync(
        Guid userId,
        Guid transactionId,
        UpdateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var transaction = await db.Transactions
            .Include(item => item.Category)
            .SingleOrDefaultAsync(item => item.Id == transactionId && item.UserId == userId, cancellationToken);
        if (transaction is null)
        {
            return (null, TransactionError.NotFound);
        }

        var category = await db.Categories.SingleOrDefaultAsync(
            candidate => candidate.Id == request.CategoryId && candidate.UserId == userId,
            cancellationToken);
        if (category is null)
        {
            return (null, TransactionError.CategoryNotFound);
        }

        var validationError = Validate(request.Amount, request.OccurredAtUtc, request.Description);
        if (validationError.HasValue)
        {
            return (null, validationError.Value);
        }

        transaction.CategoryId = category.Id;
        transaction.Category = category;
        transaction.Amount = request.Amount;
        transaction.OccurredAtUtc = NormalizeUtc(request.OccurredAtUtc);
        transaction.Description = NormalizeDescription(request.Description);
        await db.SaveChangesAsync(cancellationToken);
        return (ToResponse(transaction), null);
    }

    public async Task<TransactionError?> DeleteAsync(
        Guid userId,
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        var transaction = await db.Transactions.SingleOrDefaultAsync(
            item => item.Id == transactionId && item.UserId == userId,
            cancellationToken);
        if (transaction is null)
        {
            return TransactionError.NotFound;
        }

        db.Transactions.Remove(transaction);
        await db.SaveChangesAsync(cancellationToken);
        return null;
    }

    private static TransactionError? Validate(decimal amount, DateTime occurredAtUtc, string? description)
    {
        if (amount <= 0)
        {
            return TransactionError.InvalidAmount;
        }

        if (occurredAtUtc == default)
        {
            return TransactionError.InvalidDate;
        }

        if (description?.Length > 500)
        {
            return TransactionError.InvalidDescription;
        }

        return null;
    }

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static TransactionResponse ToResponse(FinanceTransaction transaction) =>
        new(
            transaction.Id,
            transaction.CategoryId,
            transaction.Category.Name,
            transaction.Category.Type.ToString(),
            transaction.Amount,
            transaction.OccurredAtUtc,
            transaction.Description,
            transaction.CreatedAtUtc);
}

public enum TransactionError
{
    NotFound,
    CategoryNotFound,
    InvalidAmount,
    InvalidDate,
    InvalidDescription
}

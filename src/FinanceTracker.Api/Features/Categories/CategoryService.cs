using FinanceTracker.Api.Domain;
using FinanceTracker.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Api.Features.Categories;

public sealed class CategoryService(FinanceTrackerDbContext db)
{
    public async Task<IReadOnlyList<CategoryResponse>> ListAsync(
        Guid userId,
        CategoryType? type,
        CancellationToken cancellationToken)
    {
        var query = db.Categories.AsNoTracking().Where(category => category.UserId == userId);
        if (type.HasValue)
        {
            query = query.Where(category => category.Type == type.Value);
        }

        return await query
            .OrderBy(category => category.Type)
            .ThenBy(category => category.Name)
            .Select(category => new CategoryResponse(
                category.Id,
                category.Name,
                category.Type.ToString(),
                category.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<(CategoryResponse? Response, bool Duplicate)> CreateAsync(
        Guid userId,
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var name = NormalizeName(request.Name);
        if (name is null || !Enum.TryParse<CategoryType>(request.Type, true, out var type))
        {
            return (null, false);
        }

        if (await ExistsAsync(userId, name, type, null, cancellationToken))
        {
            return (null, true);
        }

        var category = new Category
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Type = type,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Categories.Add(category);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return (null, true);
        }

        return (ToResponse(category), false);
    }

    public async Task<(CategoryResponse? Response, bool Duplicate)> UpdateAsync(
        Guid userId,
        Guid categoryId,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await db.Categories
            .SingleOrDefaultAsync(candidate => candidate.Id == categoryId && candidate.UserId == userId, cancellationToken);
        if (category is null)
        {
            return (null, false);
        }

        var name = NormalizeName(request.Name);
        if (name is null)
        {
            return (null, false);
        }

        if (await ExistsAsync(userId, name, category.Type, categoryId, cancellationToken))
        {
            return (null, true);
        }

        category.Name = name;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return (null, true);
        }

        return (ToResponse(category), false);
    }

    public async Task<DeleteCategoryResult> DeleteAsync(
        Guid userId,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var category = await db.Categories
            .SingleOrDefaultAsync(candidate => candidate.Id == categoryId && candidate.UserId == userId, cancellationToken);
        if (category is null)
        {
            return DeleteCategoryResult.NotFound;
        }

        if (await db.Transactions.AnyAsync(transaction => transaction.CategoryId == categoryId, cancellationToken))
        {
            return DeleteCategoryResult.HasTransactions;
        }

        if (await db.Budgets.AnyAsync(budget => budget.CategoryId == categoryId, cancellationToken))
        {
            return DeleteCategoryResult.HasBudgets;
        }

        db.Categories.Remove(category);
        await db.SaveChangesAsync(cancellationToken);
        return DeleteCategoryResult.Deleted;
    }

    private Task<bool> ExistsAsync(Guid userId, string name, CategoryType type, Guid? excludedId, CancellationToken cancellationToken) =>
        db.Categories.AnyAsync(category =>
            category.UserId == userId && category.Type == type && category.Name == name &&
            (!excludedId.HasValue || category.Id != excludedId.Value), cancellationToken);

    private static string? NormalizeName(string name)
    {
        var normalized = name.Trim();
        return normalized.Length is >= 1 and <= 80 ? normalized : null;
    }

    private static CategoryResponse ToResponse(Category category) =>
        new(category.Id, category.Name, category.Type.ToString(), category.CreatedAtUtc);
}

public enum DeleteCategoryResult
{
    Deleted,
    NotFound,
    HasTransactions,
    HasBudgets
}

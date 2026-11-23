using FinanceTracker.Api.Domain;
using FinanceTracker.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Api.Features.Budgets;

public sealed class BudgetService(FinanceTrackerDbContext db)
{
    public async Task<IReadOnlyList<BudgetResponse>> ListAsync(
        Guid userId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        return await db.Budgets
            .AsNoTracking()
            .Where(budget => budget.UserId == userId && budget.Year == year && budget.Month == month)
            .OrderBy(budget => budget.Category.Name)
            .Select(budget => new BudgetResponse(
                budget.Id,
                budget.CategoryId,
                budget.Category.Name,
                budget.Year,
                budget.Month,
                budget.AmountLimit))
            .ToListAsync(cancellationToken);
    }

    public async Task<(BudgetResponse? Response, BudgetError? Error)> UpsertAsync(
        Guid userId,
        Guid categoryId,
        int year,
        int month,
        UpsertBudgetRequest request,
        CancellationToken cancellationToken)
    {
        if (request.AmountLimit <= 0)
        {
            return (null, BudgetError.InvalidAmount);
        }

        var category = await db.Categories.SingleOrDefaultAsync(
            candidate => candidate.Id == categoryId && candidate.UserId == userId,
            cancellationToken);
        if (category is null)
        {
            return (null, BudgetError.CategoryNotFound);
        }

        if (category.Type != CategoryType.Expense)
        {
            return (null, BudgetError.NotExpenseCategory);
        }

        var budget = await db.Budgets.SingleOrDefaultAsync(existing =>
            existing.UserId == userId &&
            existing.CategoryId == categoryId &&
            existing.Year == year &&
            existing.Month == month, cancellationToken);

        if (budget is null)
        {
            budget = new Budget
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CategoryId = categoryId,
                Year = year,
                Month = month,
                AmountLimit = request.AmountLimit,
                Category = category
            };
            db.Budgets.Add(budget);
        }
        else
        {
            budget.AmountLimit = request.AmountLimit;
            budget.Category = category;
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return (null, BudgetError.Conflict);
        }

        return (ToResponse(budget), null);
    }

    private static BudgetResponse ToResponse(Budget budget) =>
        new(budget.Id, budget.CategoryId, budget.Category.Name, budget.Year, budget.Month, budget.AmountLimit);
}

public enum BudgetError
{
    InvalidAmount,
    CategoryNotFound,
    NotExpenseCategory,
    Conflict
}

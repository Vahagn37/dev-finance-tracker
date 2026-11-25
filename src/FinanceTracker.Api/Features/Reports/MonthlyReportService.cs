using FinanceTracker.Api.Domain;
using FinanceTracker.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Api.Features.Reports;

public sealed class MonthlyReportService(FinanceTrackerDbContext db)
{
    public async Task<MonthlyReportResponse> GetAsync(
        Guid userId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        var startUtc = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var nextMonthUtc = startUtc.AddMonths(1);

        var totals = await db.Transactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.UserId == userId &&
                transaction.OccurredAtUtc >= startUtc &&
                transaction.OccurredAtUtc < nextMonthUtc)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Income = group.Where(transaction => transaction.Category.Type == CategoryType.Income)
                    .Sum(transaction => (decimal?)transaction.Amount) ?? 0m,
                Expenses = group.Where(transaction => transaction.Category.Type == CategoryType.Expense)
                    .Sum(transaction => (decimal?)transaction.Amount) ?? 0m
            })
            .SingleOrDefaultAsync(cancellationToken);

        var spending = await db.Transactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.UserId == userId &&
                transaction.Category.Type == CategoryType.Expense &&
                transaction.OccurredAtUtc >= startUtc &&
                transaction.OccurredAtUtc < nextMonthUtc)
            .GroupBy(transaction => new { transaction.CategoryId, transaction.Category.Name })
            .Select(group => new
            {
                group.Key.CategoryId,
                group.Key.Name,
                Spent = group.Sum(transaction => transaction.Amount)
            })
            .ToListAsync(cancellationToken);

        var budgets = await db.Budgets
            .AsNoTracking()
            .Where(budget => budget.UserId == userId && budget.Year == year && budget.Month == month)
            .Select(budget => new
            {
                budget.CategoryId,
                CategoryName = budget.Category.Name,
                budget.AmountLimit
            })
            .ToListAsync(cancellationToken);

        var categoryReports = spending
            .Select(item => new
            {
                item.CategoryId,
                item.Name,
                item.Spent,
                Budget = budgets.FirstOrDefault(budget => budget.CategoryId == item.CategoryId)?.AmountLimit
            })
            .Concat(budgets
                .Where(budget => spending.All(item => item.CategoryId != budget.CategoryId))
                .Select(budget => new
                {
                    budget.CategoryId,
                    Name = budget.CategoryName,
                    Spent = 0m,
                    Budget = (decimal?)budget.AmountLimit
                }))
            .OrderBy(item => item.Name)
            .Select(item => new MonthlyCategoryReport(
                item.CategoryId,
                item.Name,
                item.Spent,
                item.Budget,
                item.Budget.HasValue ? item.Budget.Value - item.Spent : null,
                item.Budget.HasValue && item.Spent > item.Budget.Value))
            .ToList();

        var totalIncome = totals?.Income ?? 0m;
        var totalExpenses = totals?.Expenses ?? 0m;
        return new MonthlyReportResponse(
            year,
            month,
            totalIncome,
            totalExpenses,
            totalIncome - totalExpenses,
            categoryReports);
    }
}

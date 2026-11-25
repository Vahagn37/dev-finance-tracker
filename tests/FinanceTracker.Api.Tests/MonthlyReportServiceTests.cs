using FinanceTracker.Api.Domain;
using FinanceTracker.Api.Features.Reports;
using FinanceTracker.Api.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Api.Tests;

public sealed class MonthlyReportServiceTests
{
    [Fact]
    public async Task GetAsync_calculates_income_expenses_and_budget_progress()
    {
        await using var db = CreateDbContext();
        var user = new User { Id = Guid.NewGuid(), Email = "A", PasswordHash = "hash", CreatedAtUtc = DateTime.UtcNow };
        var income = new Category { Id = Guid.NewGuid(), UserId = user.Id, Name = "Salary", Type = CategoryType.Income, CreatedAtUtc = DateTime.UtcNow };
        var food = new Category { Id = Guid.NewGuid(), UserId = user.Id, Name = "Food", Type = CategoryType.Expense, CreatedAtUtc = DateTime.UtcNow };
        db.Users.Add(user);
        db.Categories.AddRange(income, food);
        db.Transactions.AddRange(
            new FinanceTransaction { Id = Guid.NewGuid(), UserId = user.Id, CategoryId = income.Id, Category = income, Amount = 3000m, OccurredAtUtc = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc), CreatedAtUtc = DateTime.UtcNow },
            new FinanceTransaction { Id = Guid.NewGuid(), UserId = user.Id, CategoryId = food.Id, Category = food, Amount = 125m, OccurredAtUtc = new DateTime(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc), CreatedAtUtc = DateTime.UtcNow });
        db.Budgets.Add(new Budget { Id = Guid.NewGuid(), UserId = user.Id, CategoryId = food.Id, Category = food, Year = 2026, Month = 11, AmountLimit = 200m });
        await db.SaveChangesAsync();

        var report = await new MonthlyReportService(db).GetAsync(user.Id, 2026, 11, CancellationToken.None);

        report.TotalIncome.Should().Be(3000m);
        report.TotalExpenses.Should().Be(125m);
        report.NetBalance.Should().Be(2875m);
        report.Categories.Should().ContainSingle().Which.Remaining.Should().Be(75m);
    }

    [Fact]
    public async Task GetAsync_returns_zero_totals_for_empty_month()
    {
        await using var db = CreateDbContext();
        var user = new User { Id = Guid.NewGuid(), Email = "A", PasswordHash = "hash", CreatedAtUtc = DateTime.UtcNow };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var report = await new MonthlyReportService(db).GetAsync(user.Id, 2026, 11, CancellationToken.None);

        report.TotalIncome.Should().Be(0m);
        report.TotalExpenses.Should().Be(0m);
        report.Categories.Should().BeEmpty();
    }

    private static FinanceTrackerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FinanceTrackerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new FinanceTrackerDbContext(options);
    }
}

using FinanceTracker.Api.Domain;
using FinanceTracker.Api.Features.Categories;
using FinanceTracker.Api.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Api.Tests;

public sealed class CategoryServiceTests
{
    [Fact]
    public async Task ListAsync_returns_only_categories_owned_by_current_user()
    {
        await using var db = CreateDbContext();
        var userA = new User { Id = Guid.NewGuid(), Email = "A", PasswordHash = "hash", CreatedAtUtc = DateTime.UtcNow };
        var userB = new User { Id = Guid.NewGuid(), Email = "B", PasswordHash = "hash", CreatedAtUtc = DateTime.UtcNow };
        db.Users.AddRange(userA, userB);
        db.Categories.AddRange(
            new Category { Id = Guid.NewGuid(), UserId = userA.Id, Name = "Food", Type = CategoryType.Expense, CreatedAtUtc = DateTime.UtcNow },
            new Category { Id = Guid.NewGuid(), UserId = userB.Id, Name = "Private", Type = CategoryType.Expense, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var result = await new CategoryService(db).ListAsync(userA.Id, null, CancellationToken.None);

        result.Should().ContainSingle().Which.Name.Should().Be("Food");
    }

    [Fact]
    public async Task CreateAsync_rejects_duplicate_name_and_type_for_same_user()
    {
        await using var db = CreateDbContext();
        var user = new User { Id = Guid.NewGuid(), Email = "A", PasswordHash = "hash", CreatedAtUtc = DateTime.UtcNow };
        db.Users.Add(user);
        db.Categories.Add(new Category { Id = Guid.NewGuid(), UserId = user.Id, Name = "Food", Type = CategoryType.Expense, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var result = await new CategoryService(db).CreateAsync(
            user.Id,
            new CreateCategoryRequest("Food", "expense"),
            CancellationToken.None);

        result.Response.Should().BeNull();
        result.Duplicate.Should().BeTrue();
    }

    private static FinanceTrackerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FinanceTrackerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new FinanceTrackerDbContext(options);
    }
}

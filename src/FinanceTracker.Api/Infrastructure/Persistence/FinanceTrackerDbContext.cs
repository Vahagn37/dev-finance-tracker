using FinanceTracker.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Api.Infrastructure.Persistence;

public sealed class FinanceTrackerDbContext(DbContextOptions<FinanceTrackerDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<FinanceTransaction> Transactions => Set<FinanceTransaction>();
    public DbSet<Budget> Budgets => Set<Budget>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Email).HasMaxLength(320).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();
            entity.HasIndex(user => user.Email).IsUnique();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Name).HasMaxLength(80).IsRequired();
            entity.Property(category => category.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.HasIndex(category => new { category.UserId, category.Name, category.Type }).IsUnique();
            entity.HasOne(category => category.User).WithMany(user => user.Categories)
                .HasForeignKey(category => category.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FinanceTransaction>(entity =>
        {
            entity.HasKey(transaction => transaction.Id);
            entity.Property(transaction => transaction.Amount).HasPrecision(18, 2).IsRequired();
            entity.Property(transaction => transaction.Description).HasMaxLength(500);
            entity.HasIndex(transaction => new { transaction.UserId, transaction.OccurredAtUtc });
            entity.HasIndex(transaction => new { transaction.UserId, transaction.CategoryId });
            entity.HasOne(transaction => transaction.User).WithMany(user => user.Transactions)
                .HasForeignKey(transaction => transaction.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(transaction => transaction.Category).WithMany(category => category.Transactions)
                .HasForeignKey(transaction => transaction.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Budget>(entity =>
        {
            entity.HasKey(budget => budget.Id);
            entity.Property(budget => budget.AmountLimit).HasPrecision(18, 2).IsRequired();
            entity.HasIndex(budget => new { budget.UserId, budget.CategoryId, budget.Year, budget.Month }).IsUnique();
            entity.HasOne(budget => budget.User).WithMany(user => user.Budgets)
                .HasForeignKey(budget => budget.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(budget => budget.Category).WithMany(category => category.Budgets)
                .HasForeignKey(budget => budget.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

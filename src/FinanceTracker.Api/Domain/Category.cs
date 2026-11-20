namespace FinanceTracker.Api.Domain;

public sealed class Category
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public CategoryType Type { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public User User { get; set; } = null!;
    public ICollection<FinanceTransaction> Transactions { get; set; } = new List<FinanceTransaction>();
    public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
}

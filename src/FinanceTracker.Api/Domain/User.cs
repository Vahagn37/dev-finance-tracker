namespace FinanceTracker.Api.Domain;

public sealed class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<FinanceTransaction> Transactions { get; set; } = new List<FinanceTransaction>();
    public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
}

namespace FinanceTracker.Api.Domain;

public sealed class FinanceTransaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid CategoryId { get; set; }
    public decimal Amount { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public User User { get; set; } = null!;
    public Category Category { get; set; } = null!;
}

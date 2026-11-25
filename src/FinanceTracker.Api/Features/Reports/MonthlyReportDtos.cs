namespace FinanceTracker.Api.Features.Reports;

public sealed record MonthlyCategoryReport(
    Guid CategoryId,
    string CategoryName,
    decimal Spent,
    decimal? Budget,
    decimal? Remaining,
    bool IsOverBudget);

public sealed record MonthlyReportResponse(
    int Year,
    int Month,
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal NetBalance,
    IReadOnlyList<MonthlyCategoryReport> Categories);

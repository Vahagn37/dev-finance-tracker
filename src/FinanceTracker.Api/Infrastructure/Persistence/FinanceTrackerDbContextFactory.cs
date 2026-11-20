using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FinanceTracker.Api.Infrastructure.Persistence;

public sealed class FinanceTrackerDbContextFactory : IDesignTimeDbContextFactory<FinanceTrackerDbContext>
{
    public FinanceTrackerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FinanceTrackerDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=54329;Database=finance_tracker;Username=finance_tracker;Password=finance_tracker_dev_password");
        return new FinanceTrackerDbContext(optionsBuilder.Options);
    }
}

using Eatopia.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace Eatopia.Tests.Integration;

public class DatabaseSchemaTests
{
    [Fact]
    public async Task Empty_database_schema_contains_predeploy_critical_tables()
    {
        using var factory = new EatopiaApiFactory();
        await factory.ResetDatabaseAsync();

        var tableNames = await factory.WithDbAsync(async db =>
        {
            return await db.Database.SqlQueryRaw<string>(
                    "SELECT name AS Value FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'")
                .ToListAsync();
        });

        Assert.Contains("Users", tableNames);
        Assert.Contains("Recipes", tableNames);
        Assert.Contains("ContentReports", tableNames);
        Assert.Contains("Notifications", tableNames);
        Assert.Contains("ChatThreads", tableNames);
        Assert.Contains("ChatMessages", tableNames);
        Assert.Contains("DietPlans", tableNames);
        Assert.Contains("DietPlanItems", tableNames);
    }
}

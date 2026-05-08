using Microsoft.EntityFrameworkCore;

namespace Eatopia.Infrastructure.Persistence;

public static class DatabaseSchemaRepair
{
    public static void Apply(EatopiaDbContext db)
    {
        // Disabled for PostgreSQL compatibility
    }
}
using Microsoft.EntityFrameworkCore;

namespace Eatopia.Infrastructure.Persistence;

public static class CommunityAuthSchemaRepair
{
    public static void Apply(EatopiaDbContext db)
    {
        // Disabled for PostgreSQL
    }
}
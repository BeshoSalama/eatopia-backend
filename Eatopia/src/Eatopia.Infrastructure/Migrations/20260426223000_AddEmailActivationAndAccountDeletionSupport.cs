using Eatopia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eatopia.Infrastructure.Migrations
{
    [DbContext(typeof(EatopiaDbContext))]
    [Migration("20260426223000_AddEmailActivationAndAccountDeletionSupport")]
    public partial class AddEmailActivationAndAccountDeletionSupport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'dbo.Users', N'EmailConfirmed') IS NULL
BEGIN
    ALTER TABLE [Users] ADD [EmailConfirmed] bit NOT NULL CONSTRAINT [DF_Users_EmailConfirmed_Activation] DEFAULT CAST(0 AS bit);
END;
");

            migrationBuilder.Sql(@"
IF COL_LENGTH(N'dbo.Users', N'EmailConfirmedAt') IS NULL
BEGIN
    ALTER TABLE [Users] ADD [EmailConfirmedAt] datetime2 NULL;
END;
");

            migrationBuilder.Sql(@"
IF COL_LENGTH(N'dbo.Users', N'EmailConfirmationTokenHash') IS NULL
BEGIN
    ALTER TABLE [Users] ADD [EmailConfirmationTokenHash] nvarchar(max) NULL;
END;
");

            migrationBuilder.Sql(@"
IF COL_LENGTH(N'dbo.Users', N'EmailConfirmationTokenExpiresAt') IS NULL
BEGIN
    ALTER TABLE [Users] ADD [EmailConfirmationTokenExpiresAt] datetime2 NULL;
END;
");

            migrationBuilder.Sql(@"
IF COL_LENGTH(N'dbo.Users', N'LastEmailConfirmationSentAt') IS NULL
BEGIN
    ALTER TABLE [Users] ADD [LastEmailConfirmationSentAt] datetime2 NULL;
END;
");

            // Dynamic SQL prevents SQL Server from compiling references to columns
            // before the ALTER TABLE statements above have taken effect.
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'dbo.Users', N'EmailConfirmed') IS NOT NULL
   AND COL_LENGTH(N'dbo.Users', N'EmailConfirmedAt') IS NOT NULL
   AND COL_LENGTH(N'dbo.Users', N'EmailConfirmationTokenHash') IS NOT NULL
BEGIN
    EXEC(N'
        UPDATE [Users]
        SET [EmailConfirmed] = CAST(1 AS bit),
            [EmailConfirmedAt] = COALESCE([EmailConfirmedAt], SYSUTCDATETIME())
        WHERE [EmailConfirmed] = CAST(0 AS bit)
          AND [EmailConfirmationTokenHash] IS NULL;
    ');
END;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'dbo.Users', N'LastEmailConfirmationSentAt') IS NOT NULL ALTER TABLE [Users] DROP COLUMN [LastEmailConfirmationSentAt];
IF COL_LENGTH(N'dbo.Users', N'EmailConfirmationTokenExpiresAt') IS NOT NULL ALTER TABLE [Users] DROP COLUMN [EmailConfirmationTokenExpiresAt];
IF COL_LENGTH(N'dbo.Users', N'EmailConfirmationTokenHash') IS NOT NULL ALTER TABLE [Users] DROP COLUMN [EmailConfirmationTokenHash];
IF COL_LENGTH(N'dbo.Users', N'EmailConfirmedAt') IS NOT NULL ALTER TABLE [Users] DROP COLUMN [EmailConfirmedAt];
");
        }
    }
}

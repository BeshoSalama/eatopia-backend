using Eatopia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eatopia.Infrastructure.Migrations
{
    [DbContext(typeof(EatopiaDbContext))]
    [Migration("20260425214000_AddNotificationsSocialWaterReminders")]
    public partial class AddNotificationsSocialWaterReminders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'dbo.Users', N'AuthProvider') IS NULL
BEGIN
    ALTER TABLE [Users] ADD [AuthProvider] nvarchar(50) NOT NULL CONSTRAINT [DF_Users_AuthProvider] DEFAULT N'Local';
END;
IF COL_LENGTH(N'dbo.Users', N'EmailConfirmed') IS NULL
BEGIN
    ALTER TABLE [Users] ADD [EmailConfirmed] bit NOT NULL CONSTRAINT [DF_Users_EmailConfirmed] DEFAULT CAST(0 AS bit);
END;
IF COL_LENGTH(N'dbo.Users', N'ExternalProviderId') IS NULL
BEGIN
    ALTER TABLE [Users] ADD [ExternalProviderId] nvarchar(300) NULL;
END;
IF COL_LENGTH(N'dbo.Medications', N'BeforeAfterMeal') IS NULL
BEGIN
    ALTER TABLE [Medications] ADD [BeforeAfterMeal] nvarchar(20) NULL;
END;
IF OBJECT_ID(N'dbo.Notifications', N'U') IS NULL
BEGIN
    CREATE TABLE [Notifications] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Message] nvarchar(max) NOT NULL,
        [Type] nvarchar(50) NOT NULL CONSTRAINT [DF_Notifications_Type] DEFAULT N'info',
        [IsRead] bit NOT NULL CONSTRAINT [DF_Notifications_IsRead] DEFAULT CAST(0 AS bit),
        [ReadAt] datetime2 NULL,
        [ScheduledFor] datetime2 NULL,
        [RelatedEntityType] nvarchar(100) NULL,
        [RelatedEntityId] uniqueidentifier NULL,
        [EmailSent] bit NOT NULL CONSTRAINT [DF_Notifications_EmailSent] DEFAULT CAST(0 AS bit),
        [EmailSentAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Notifications_UserId_IsRead_CreatedAt' AND object_id = OBJECT_ID(N'dbo.Notifications'))
BEGIN
    CREATE INDEX [IX_Notifications_UserId_IsRead_CreatedAt] ON [Notifications] ([UserId], [IsRead], [CreatedAt]);
END;
IF OBJECT_ID(N'dbo.WaterReminders', N'U') IS NULL
BEGIN
    CREATE TABLE [WaterReminders] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [ReminderDate] datetime2 NOT NULL,
        [TimeOfDay] time NOT NULL,
        [AmountMl] int NOT NULL,
        [IsCompleted] bit NOT NULL CONSTRAINT [DF_WaterReminders_IsCompleted] DEFAULT CAST(0 AS bit),
        [CompletedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_WaterReminders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WaterReminders_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_WaterReminders_UserId_ReminderDate_TimeOfDay' AND object_id = OBJECT_ID(N'dbo.WaterReminders'))
BEGIN
    CREATE INDEX [IX_WaterReminders_UserId_ReminderDate_TimeOfDay] ON [WaterReminders] ([UserId], [ReminderDate], [TimeOfDay]);
END;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'dbo.WaterReminders', N'U') IS NOT NULL DROP TABLE [WaterReminders];
IF OBJECT_ID(N'dbo.Notifications', N'U') IS NOT NULL DROP TABLE [Notifications];
IF COL_LENGTH(N'dbo.Medications', N'BeforeAfterMeal') IS NOT NULL ALTER TABLE [Medications] DROP COLUMN [BeforeAfterMeal];
IF COL_LENGTH(N'dbo.Users', N'ExternalProviderId') IS NOT NULL ALTER TABLE [Users] DROP COLUMN [ExternalProviderId];
IF COL_LENGTH(N'dbo.Users', N'EmailConfirmed') IS NOT NULL ALTER TABLE [Users] DROP COLUMN [EmailConfirmed];
IF COL_LENGTH(N'dbo.Users', N'AuthProvider') IS NOT NULL ALTER TABLE [Users] DROP COLUMN [AuthProvider];
");
        }
    }
}

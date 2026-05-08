using Microsoft.EntityFrameworkCore;

namespace Eatopia.Infrastructure.Persistence;

public static class DatabaseSchemaRepair
{
    public static void Apply(EatopiaDbContext db)
    {
        db.Database.ExecuteSqlRaw(@"
SET NOCOUNT ON;

/* Users: profile + social login columns */
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Users', N'Username') IS NULL ALTER TABLE [Users] ADD [Username] nvarchar(100) NULL;
    IF COL_LENGTH(N'dbo.Users', N'BirthDate') IS NULL ALTER TABLE [Users] ADD [BirthDate] datetime2 NULL;
    IF COL_LENGTH(N'dbo.Users', N'Location') IS NULL ALTER TABLE [Users] ADD [Location] nvarchar(100) NULL;
    IF COL_LENGTH(N'dbo.Users', N'Phone') IS NULL ALTER TABLE [Users] ADD [Phone] nvarchar(30) NULL;
    IF COL_LENGTH(N'dbo.Users', N'ProfileImageUrl') IS NULL ALTER TABLE [Users] ADD [ProfileImageUrl] nvarchar(max) NULL;
    IF COL_LENGTH(N'dbo.Users', N'Age') IS NULL ALTER TABLE [Users] ADD [Age] int NULL;
    IF COL_LENGTH(N'dbo.Users', N'WeightKg') IS NULL ALTER TABLE [Users] ADD [WeightKg] decimal(18,2) NULL;
    IF COL_LENGTH(N'dbo.Users', N'HeightCm') IS NULL ALTER TABLE [Users] ADD [HeightCm] decimal(18,2) NULL;
    IF COL_LENGTH(N'dbo.Users', N'Gender') IS NULL ALTER TABLE [Users] ADD [Gender] nvarchar(20) NULL;
    IF COL_LENGTH(N'dbo.Users', N'ActivityLevel') IS NULL ALTER TABLE [Users] ADD [ActivityLevel] nvarchar(50) NULL;
    IF COL_LENGTH(N'dbo.Users', N'Goal') IS NULL ALTER TABLE [Users] ADD [Goal] nvarchar(200) NULL;
    IF COL_LENGTH(N'dbo.Users', N'Role') IS NULL ALTER TABLE [Users] ADD [Role] nvarchar(20) NOT NULL CONSTRAINT [DF_Users_Role_Repair] DEFAULT N'User';
    IF COL_LENGTH(N'dbo.Users', N'IsBanned') IS NULL ALTER TABLE [Users] ADD [IsBanned] bit NOT NULL CONSTRAINT [DF_Users_IsBanned_Repair] DEFAULT CAST(0 AS bit);
    IF COL_LENGTH(N'dbo.Users', N'BannedAt') IS NULL ALTER TABLE [Users] ADD [BannedAt] datetime2 NULL;
    IF COL_LENGTH(N'dbo.Users', N'BannedReason') IS NULL ALTER TABLE [Users] ADD [BannedReason] nvarchar(1000) NULL;
    IF COL_LENGTH(N'dbo.Users', N'AuthProvider') IS NULL ALTER TABLE [Users] ADD [AuthProvider] nvarchar(50) NOT NULL CONSTRAINT [DF_Users_AuthProvider_Repair] DEFAULT N'Local';
    IF COL_LENGTH(N'dbo.Users', N'EmailConfirmed') IS NULL ALTER TABLE [Users] ADD [EmailConfirmed] bit NOT NULL CONSTRAINT [DF_Users_EmailConfirmed_Repair] DEFAULT CAST(0 AS bit);
    IF COL_LENGTH(N'dbo.Users', N'ExternalProviderId') IS NULL ALTER TABLE [Users] ADD [ExternalProviderId] nvarchar(300) NULL;
END;

/* Notifications */
IF OBJECT_ID(N'dbo.Notifications', N'U') IS NULL
BEGIN
    CREATE TABLE [Notifications] (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_Notifications_Id_Repair] DEFAULT NEWID(),
        [UserId] uniqueidentifier NOT NULL,
        [ActorUserId] uniqueidentifier NULL,
        [Title] nvarchar(200) NOT NULL CONSTRAINT [DF_Notifications_Title_Repair] DEFAULT N'',
        [Message] nvarchar(max) NOT NULL CONSTRAINT [DF_Notifications_Message_Repair] DEFAULT N'',
        [Type] nvarchar(50) NOT NULL CONSTRAINT [DF_Notifications_Type_Repair] DEFAULT N'info',
        [IsRead] bit NOT NULL CONSTRAINT [DF_Notifications_IsRead_Repair] DEFAULT CAST(0 AS bit),
        [ReadAt] datetime2 NULL,
        [ScheduledFor] datetime2 NULL,
        [RelatedEntityType] nvarchar(100) NULL,
        [RelatedEntityId] uniqueidentifier NULL,
        [ActionUrl] nvarchar(500) NULL,
        [EmailSent] bit NOT NULL CONSTRAINT [DF_Notifications_EmailSent_Repair] DEFAULT CAST(0 AS bit),
        [EmailSentAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_Notifications_CreatedAt_Repair] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id])
    );
END
ELSE
BEGIN
    IF COL_LENGTH(N'dbo.Notifications', N'Id') IS NULL ALTER TABLE [Notifications] ADD [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_Notifications_Id_Repair] DEFAULT NEWID();
    IF COL_LENGTH(N'dbo.Notifications', N'UserId') IS NULL ALTER TABLE [Notifications] ADD [UserId] uniqueidentifier NOT NULL CONSTRAINT [DF_Notifications_UserId_Repair] DEFAULT '00000000-0000-0000-0000-000000000000';
    IF COL_LENGTH(N'dbo.Notifications', N'ActorUserId') IS NULL ALTER TABLE [Notifications] ADD [ActorUserId] uniqueidentifier NULL;
    IF COL_LENGTH(N'dbo.Notifications', N'Title') IS NULL ALTER TABLE [Notifications] ADD [Title] nvarchar(200) NOT NULL CONSTRAINT [DF_Notifications_Title_Repair] DEFAULT N'';
    IF COL_LENGTH(N'dbo.Notifications', N'Message') IS NULL ALTER TABLE [Notifications] ADD [Message] nvarchar(max) NOT NULL CONSTRAINT [DF_Notifications_Message_Repair] DEFAULT N'';
    IF COL_LENGTH(N'dbo.Notifications', N'Type') IS NULL ALTER TABLE [Notifications] ADD [Type] nvarchar(50) NOT NULL CONSTRAINT [DF_Notifications_Type_Repair] DEFAULT N'info';
    IF COL_LENGTH(N'dbo.Notifications', N'IsRead') IS NULL ALTER TABLE [Notifications] ADD [IsRead] bit NOT NULL CONSTRAINT [DF_Notifications_IsRead_Repair] DEFAULT CAST(0 AS bit);
    IF COL_LENGTH(N'dbo.Notifications', N'ReadAt') IS NULL ALTER TABLE [Notifications] ADD [ReadAt] datetime2 NULL;
    IF COL_LENGTH(N'dbo.Notifications', N'ScheduledFor') IS NULL ALTER TABLE [Notifications] ADD [ScheduledFor] datetime2 NULL;
    IF COL_LENGTH(N'dbo.Notifications', N'RelatedEntityType') IS NULL ALTER TABLE [Notifications] ADD [RelatedEntityType] nvarchar(100) NULL;
    IF COL_LENGTH(N'dbo.Notifications', N'RelatedEntityId') IS NULL ALTER TABLE [Notifications] ADD [RelatedEntityId] uniqueidentifier NULL;
    IF COL_LENGTH(N'dbo.Notifications', N'ActionUrl') IS NULL ALTER TABLE [Notifications] ADD [ActionUrl] nvarchar(500) NULL;
    IF COL_LENGTH(N'dbo.Notifications', N'EmailSent') IS NULL ALTER TABLE [Notifications] ADD [EmailSent] bit NOT NULL CONSTRAINT [DF_Notifications_EmailSent_Repair] DEFAULT CAST(0 AS bit);
    IF COL_LENGTH(N'dbo.Notifications', N'EmailSentAt') IS NULL ALTER TABLE [Notifications] ADD [EmailSentAt] datetime2 NULL;
    IF COL_LENGTH(N'dbo.Notifications', N'CreatedAt') IS NULL ALTER TABLE [Notifications] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_Notifications_CreatedAt_Repair] DEFAULT SYSUTCDATETIME();
END;

IF OBJECT_ID(N'dbo.Notifications', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.Notifications') AND [type] = 'PK')
        ALTER TABLE [Notifications] ADD CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Notifications_UserId_IsRead_CreatedAt' AND object_id = OBJECT_ID(N'dbo.Notifications'))
        CREATE INDEX [IX_Notifications_UserId_IsRead_CreatedAt] ON [Notifications] ([UserId], [IsRead], [CreatedAt]);
END;

/* Recipes */
IF OBJECT_ID(N'dbo.Recipes', N'U') IS NULL
BEGIN
    CREATE TABLE [Recipes] (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_Recipes_Id_Repair] DEFAULT NEWID(),
        [Title] nvarchar(200) NOT NULL CONSTRAINT [DF_Recipes_Title_Repair] DEFAULT N'',
        [Description] nvarchar(1000) NULL,
        [ImageUrl] nvarchar(2000) NULL,
        [CaloriesPerServing] decimal(18,2) NULL,
        [Servings] int NOT NULL CONSTRAINT [DF_Recipes_Servings_Repair] DEFAULT 1,
        [IngredientsJson] nvarchar(max) NOT NULL CONSTRAINT [DF_Recipes_IngredientsJson_Repair] DEFAULT N'[]',
        [StepsJson] nvarchar(max) NOT NULL CONSTRAINT [DF_Recipes_StepsJson_Repair] DEFAULT N'[]',
        [AuthorId] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_Recipes_CreatedAt_Repair] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_Recipes] PRIMARY KEY ([Id])
    );
END
ELSE
BEGIN
    IF COL_LENGTH(N'dbo.Recipes', N'Id') IS NULL ALTER TABLE [Recipes] ADD [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_Recipes_Id_Repair] DEFAULT NEWID();
    IF COL_LENGTH(N'dbo.Recipes', N'Title') IS NULL ALTER TABLE [Recipes] ADD [Title] nvarchar(200) NOT NULL CONSTRAINT [DF_Recipes_Title_Repair] DEFAULT N'';
    IF COL_LENGTH(N'dbo.Recipes', N'Description') IS NULL ALTER TABLE [Recipes] ADD [Description] nvarchar(1000) NULL;
    IF COL_LENGTH(N'dbo.Recipes', N'ImageUrl') IS NULL ALTER TABLE [Recipes] ADD [ImageUrl] nvarchar(2000) NULL;
    IF COL_LENGTH(N'dbo.Recipes', N'CaloriesPerServing') IS NULL ALTER TABLE [Recipes] ADD [CaloriesPerServing] decimal(18,2) NULL;
    IF COL_LENGTH(N'dbo.Recipes', N'Servings') IS NULL ALTER TABLE [Recipes] ADD [Servings] int NOT NULL CONSTRAINT [DF_Recipes_Servings_Repair] DEFAULT 1;
    IF COL_LENGTH(N'dbo.Recipes', N'IngredientsJson') IS NULL ALTER TABLE [Recipes] ADD [IngredientsJson] nvarchar(max) NOT NULL CONSTRAINT [DF_Recipes_IngredientsJson_Repair] DEFAULT N'[]';
    IF COL_LENGTH(N'dbo.Recipes', N'StepsJson') IS NULL ALTER TABLE [Recipes] ADD [StepsJson] nvarchar(max) NOT NULL CONSTRAINT [DF_Recipes_StepsJson_Repair] DEFAULT N'[]';
    IF COL_LENGTH(N'dbo.Recipes', N'AuthorId') IS NULL ALTER TABLE [Recipes] ADD [AuthorId] uniqueidentifier NULL;
    IF COL_LENGTH(N'dbo.Recipes', N'CreatedAt') IS NULL ALTER TABLE [Recipes] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_Recipes_CreatedAt_Repair] DEFAULT SYSUTCDATETIME();
END;
IF OBJECT_ID(N'dbo.Recipes', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.Recipes') AND [type] = 'PK')
        ALTER TABLE [Recipes] ADD CONSTRAINT [PK_Recipes] PRIMARY KEY ([Id]);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Recipes_Title' AND object_id = OBJECT_ID(N'dbo.Recipes'))
        CREATE INDEX [IX_Recipes_Title] ON [Recipes] ([Title]);
END;

/* Water logs */
IF OBJECT_ID(N'dbo.WaterLogs', N'U') IS NULL
BEGIN
    CREATE TABLE [WaterLogs] (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_WaterLogs_Id_Repair] DEFAULT NEWID(),
        [UserId] uniqueidentifier NOT NULL,
        [AmountMl] int NOT NULL CONSTRAINT [DF_WaterLogs_AmountMl_Repair] DEFAULT 0,
        [LoggedAt] datetime2 NOT NULL CONSTRAINT [DF_WaterLogs_LoggedAt_Repair] DEFAULT SYSUTCDATETIME(),
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_WaterLogs_CreatedAt_Repair] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_WaterLogs] PRIMARY KEY ([Id])
    );
END
ELSE
BEGIN
    IF COL_LENGTH(N'dbo.WaterLogs', N'Id') IS NULL ALTER TABLE [WaterLogs] ADD [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_WaterLogs_Id_Repair] DEFAULT NEWID();
    IF COL_LENGTH(N'dbo.WaterLogs', N'UserId') IS NULL ALTER TABLE [WaterLogs] ADD [UserId] uniqueidentifier NOT NULL CONSTRAINT [DF_WaterLogs_UserId_Repair] DEFAULT '00000000-0000-0000-0000-000000000000';
    IF COL_LENGTH(N'dbo.WaterLogs', N'AmountMl') IS NULL ALTER TABLE [WaterLogs] ADD [AmountMl] int NOT NULL CONSTRAINT [DF_WaterLogs_AmountMl_Repair] DEFAULT 0;
    IF COL_LENGTH(N'dbo.WaterLogs', N'LoggedAt') IS NULL ALTER TABLE [WaterLogs] ADD [LoggedAt] datetime2 NOT NULL CONSTRAINT [DF_WaterLogs_LoggedAt_Repair] DEFAULT SYSUTCDATETIME();
    IF COL_LENGTH(N'dbo.WaterLogs', N'CreatedAt') IS NULL ALTER TABLE [WaterLogs] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_WaterLogs_CreatedAt_Repair] DEFAULT SYSUTCDATETIME();
END;
IF OBJECT_ID(N'dbo.WaterLogs', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.WaterLogs') AND [type] = 'PK')
        ALTER TABLE [WaterLogs] ADD CONSTRAINT [PK_WaterLogs] PRIMARY KEY ([Id]);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_WaterLogs_UserId_LoggedAt' AND object_id = OBJECT_ID(N'dbo.WaterLogs'))
        CREATE INDEX [IX_WaterLogs_UserId_LoggedAt] ON [WaterLogs] ([UserId], [LoggedAt]);
END;

/* Water reminders */
IF OBJECT_ID(N'dbo.WaterReminders', N'U') IS NULL
BEGIN
    CREATE TABLE [WaterReminders] (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_WaterReminders_Id_Repair] DEFAULT NEWID(),
        [UserId] uniqueidentifier NOT NULL,
        [ReminderDate] datetime2 NOT NULL CONSTRAINT [DF_WaterReminders_ReminderDate_Repair] DEFAULT CONVERT(date, SYSUTCDATETIME()),
        [TimeOfDay] time NOT NULL CONSTRAINT [DF_WaterReminders_TimeOfDay_Repair] DEFAULT '08:00:00',
        [AmountMl] int NOT NULL CONSTRAINT [DF_WaterReminders_AmountMl_Repair] DEFAULT 500,
        [IsCompleted] bit NOT NULL CONSTRAINT [DF_WaterReminders_IsCompleted_Repair] DEFAULT CAST(0 AS bit),
        [CompletedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_WaterReminders_CreatedAt_Repair] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_WaterReminders] PRIMARY KEY ([Id])
    );
END
ELSE
BEGIN
    IF COL_LENGTH(N'dbo.WaterReminders', N'Id') IS NULL ALTER TABLE [WaterReminders] ADD [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_WaterReminders_Id_Repair] DEFAULT NEWID();
    IF COL_LENGTH(N'dbo.WaterReminders', N'UserId') IS NULL ALTER TABLE [WaterReminders] ADD [UserId] uniqueidentifier NOT NULL CONSTRAINT [DF_WaterReminders_UserId_Repair] DEFAULT '00000000-0000-0000-0000-000000000000';
    IF COL_LENGTH(N'dbo.WaterReminders', N'ReminderDate') IS NULL ALTER TABLE [WaterReminders] ADD [ReminderDate] datetime2 NOT NULL CONSTRAINT [DF_WaterReminders_ReminderDate_Repair] DEFAULT CONVERT(date, SYSUTCDATETIME());
    IF COL_LENGTH(N'dbo.WaterReminders', N'TimeOfDay') IS NULL ALTER TABLE [WaterReminders] ADD [TimeOfDay] time NOT NULL CONSTRAINT [DF_WaterReminders_TimeOfDay_Repair] DEFAULT '08:00:00';
    IF COL_LENGTH(N'dbo.WaterReminders', N'AmountMl') IS NULL ALTER TABLE [WaterReminders] ADD [AmountMl] int NOT NULL CONSTRAINT [DF_WaterReminders_AmountMl_Repair] DEFAULT 500;
    IF COL_LENGTH(N'dbo.WaterReminders', N'IsCompleted') IS NULL ALTER TABLE [WaterReminders] ADD [IsCompleted] bit NOT NULL CONSTRAINT [DF_WaterReminders_IsCompleted_Repair] DEFAULT CAST(0 AS bit);
    IF COL_LENGTH(N'dbo.WaterReminders', N'CompletedAt') IS NULL ALTER TABLE [WaterReminders] ADD [CompletedAt] datetime2 NULL;
    IF COL_LENGTH(N'dbo.WaterReminders', N'CreatedAt') IS NULL ALTER TABLE [WaterReminders] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_WaterReminders_CreatedAt_Repair] DEFAULT SYSUTCDATETIME();
END;
IF OBJECT_ID(N'dbo.WaterReminders', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.WaterReminders') AND [type] = 'PK')
        ALTER TABLE [WaterReminders] ADD CONSTRAINT [PK_WaterReminders] PRIMARY KEY ([Id]);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_WaterReminders_UserId_ReminderDate_TimeOfDay' AND object_id = OBJECT_ID(N'dbo.WaterReminders'))
        CREATE INDEX [IX_WaterReminders_UserId_ReminderDate_TimeOfDay] ON [WaterReminders] ([UserId], [ReminderDate], [TimeOfDay]);
END;

/* Medications */
IF OBJECT_ID(N'dbo.Medications', N'U') IS NULL
BEGIN
    CREATE TABLE [Medications] (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_Medications_Id_Repair] DEFAULT NEWID(),
        [UserId] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL CONSTRAINT [DF_Medications_Name_Repair] DEFAULT N'',
        [DosageText] nvarchar(200) NULL,
        [BeforeAfterMeal] nvarchar(20) NULL,
        [TimesPerDay] int NOT NULL CONSTRAINT [DF_Medications_TimesPerDay_Repair] DEFAULT 1,
        [StartDate] datetime2 NULL,
        [EndDate] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_Medications_CreatedAt_Repair] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_Medications] PRIMARY KEY ([Id])
    );
END
ELSE
BEGIN
    IF COL_LENGTH(N'dbo.Medications', N'Id') IS NULL ALTER TABLE [Medications] ADD [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_Medications_Id_Repair] DEFAULT NEWID();
    IF COL_LENGTH(N'dbo.Medications', N'UserId') IS NULL ALTER TABLE [Medications] ADD [UserId] uniqueidentifier NOT NULL CONSTRAINT [DF_Medications_UserId_Repair] DEFAULT '00000000-0000-0000-0000-000000000000';
    IF COL_LENGTH(N'dbo.Medications', N'Name') IS NULL ALTER TABLE [Medications] ADD [Name] nvarchar(200) NOT NULL CONSTRAINT [DF_Medications_Name_Repair] DEFAULT N'';
    IF COL_LENGTH(N'dbo.Medications', N'DosageText') IS NULL ALTER TABLE [Medications] ADD [DosageText] nvarchar(200) NULL;
    IF COL_LENGTH(N'dbo.Medications', N'BeforeAfterMeal') IS NULL ALTER TABLE [Medications] ADD [BeforeAfterMeal] nvarchar(20) NULL;
    IF COL_LENGTH(N'dbo.Medications', N'TimesPerDay') IS NULL ALTER TABLE [Medications] ADD [TimesPerDay] int NOT NULL CONSTRAINT [DF_Medications_TimesPerDay_Repair] DEFAULT 1;
    IF COL_LENGTH(N'dbo.Medications', N'StartDate') IS NULL ALTER TABLE [Medications] ADD [StartDate] datetime2 NULL;
    IF COL_LENGTH(N'dbo.Medications', N'EndDate') IS NULL ALTER TABLE [Medications] ADD [EndDate] datetime2 NULL;
    IF COL_LENGTH(N'dbo.Medications', N'CreatedAt') IS NULL ALTER TABLE [Medications] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_Medications_CreatedAt_Repair] DEFAULT SYSUTCDATETIME();
END;
IF OBJECT_ID(N'dbo.Medications', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.Medications') AND [type] = 'PK')
        ALTER TABLE [Medications] ADD CONSTRAINT [PK_Medications] PRIMARY KEY ([Id]);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Medications_UserId' AND object_id = OBJECT_ID(N'dbo.Medications'))
        CREATE INDEX [IX_Medications_UserId] ON [Medications] ([UserId]);
END;

/* Medication schedules */
IF OBJECT_ID(N'dbo.MedicationSchedules', N'U') IS NULL
BEGIN
    CREATE TABLE [MedicationSchedules] (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_MedicationSchedules_Id_Repair] DEFAULT NEWID(),
        [MedicationId] uniqueidentifier NOT NULL,
        [ScheduledDate] datetime2 NOT NULL CONSTRAINT [DF_MedicationSchedules_ScheduledDate_Repair] DEFAULT CONVERT(date, SYSUTCDATETIME()),
        [TimeOfDay] time NOT NULL CONSTRAINT [DF_MedicationSchedules_TimeOfDay_Repair] DEFAULT '08:00:00',
        [IsTaken] bit NOT NULL CONSTRAINT [DF_MedicationSchedules_IsTaken_Repair] DEFAULT CAST(0 AS bit),
        [TakenAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_MedicationSchedules_CreatedAt_Repair] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_MedicationSchedules] PRIMARY KEY ([Id])
    );
END
ELSE
BEGIN
    IF COL_LENGTH(N'dbo.MedicationSchedules', N'Id') IS NULL ALTER TABLE [MedicationSchedules] ADD [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_MedicationSchedules_Id_Repair] DEFAULT NEWID();
    IF COL_LENGTH(N'dbo.MedicationSchedules', N'MedicationId') IS NULL ALTER TABLE [MedicationSchedules] ADD [MedicationId] uniqueidentifier NOT NULL CONSTRAINT [DF_MedicationSchedules_MedicationId_Repair] DEFAULT '00000000-0000-0000-0000-000000000000';
    IF COL_LENGTH(N'dbo.MedicationSchedules', N'ScheduledDate') IS NULL ALTER TABLE [MedicationSchedules] ADD [ScheduledDate] datetime2 NOT NULL CONSTRAINT [DF_MedicationSchedules_ScheduledDate_Repair] DEFAULT CONVERT(date, SYSUTCDATETIME());
    IF COL_LENGTH(N'dbo.MedicationSchedules', N'TimeOfDay') IS NULL ALTER TABLE [MedicationSchedules] ADD [TimeOfDay] time NOT NULL CONSTRAINT [DF_MedicationSchedules_TimeOfDay_Repair] DEFAULT '08:00:00';
    IF COL_LENGTH(N'dbo.MedicationSchedules', N'IsTaken') IS NULL ALTER TABLE [MedicationSchedules] ADD [IsTaken] bit NOT NULL CONSTRAINT [DF_MedicationSchedules_IsTaken_Repair] DEFAULT CAST(0 AS bit);
    IF COL_LENGTH(N'dbo.MedicationSchedules', N'TakenAt') IS NULL ALTER TABLE [MedicationSchedules] ADD [TakenAt] datetime2 NULL;
    IF COL_LENGTH(N'dbo.MedicationSchedules', N'CreatedAt') IS NULL ALTER TABLE [MedicationSchedules] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_MedicationSchedules_CreatedAt_Repair] DEFAULT SYSUTCDATETIME();
END;
IF OBJECT_ID(N'dbo.MedicationSchedules', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.MedicationSchedules') AND [type] = 'PK')
        ALTER TABLE [MedicationSchedules] ADD CONSTRAINT [PK_MedicationSchedules] PRIMARY KEY ([Id]);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MedicationSchedules_MedicationId_ScheduledDate' AND object_id = OBJECT_ID(N'dbo.MedicationSchedules'))
        CREATE INDEX [IX_MedicationSchedules_MedicationId_ScheduledDate] ON [MedicationSchedules] ([MedicationId], [ScheduledDate]);
END;

/* Food + meal logs */
IF OBJECT_ID(N'dbo.FoodItems', N'U') IS NULL
BEGIN
    CREATE TABLE [FoodItems] (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_FoodItems_Id_Repair] DEFAULT NEWID(),
        [Name] nvarchar(200) NOT NULL,
        [CaloriesPer100g] decimal(18,2) NOT NULL CONSTRAINT [DF_FoodItems_Calories_Repair] DEFAULT 0,
        [ProteinPer100g] decimal(18,2) NOT NULL CONSTRAINT [DF_FoodItems_Protein_Repair] DEFAULT 0,
        [FatPer100g] decimal(18,2) NOT NULL CONSTRAINT [DF_FoodItems_Fat_Repair] DEFAULT 0,
        [CarbsPer100g] decimal(18,2) NOT NULL CONSTRAINT [DF_FoodItems_Carbs_Repair] DEFAULT 0,
        [ServingSize] nvarchar(100) NULL,
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_FoodItems_CreatedAt_Repair] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_FoodItems] PRIMARY KEY ([Id])
    );
END
ELSE
BEGIN
    IF COL_LENGTH(N'dbo.FoodItems', N'Id') IS NULL ALTER TABLE [FoodItems] ADD [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_FoodItems_Id_Repair] DEFAULT NEWID();
    IF COL_LENGTH(N'dbo.FoodItems', N'Name') IS NULL ALTER TABLE [FoodItems] ADD [Name] nvarchar(200) NOT NULL CONSTRAINT [DF_FoodItems_Name_Repair] DEFAULT N'';
    IF COL_LENGTH(N'dbo.FoodItems', N'CaloriesPer100g') IS NULL ALTER TABLE [FoodItems] ADD [CaloriesPer100g] decimal(18,2) NOT NULL CONSTRAINT [DF_FoodItems_Calories_Repair] DEFAULT 0;
    IF COL_LENGTH(N'dbo.FoodItems', N'ProteinPer100g') IS NULL ALTER TABLE [FoodItems] ADD [ProteinPer100g] decimal(18,2) NOT NULL CONSTRAINT [DF_FoodItems_Protein_Repair] DEFAULT 0;
    IF COL_LENGTH(N'dbo.FoodItems', N'FatPer100g') IS NULL ALTER TABLE [FoodItems] ADD [FatPer100g] decimal(18,2) NOT NULL CONSTRAINT [DF_FoodItems_Fat_Repair] DEFAULT 0;
    IF COL_LENGTH(N'dbo.FoodItems', N'CarbsPer100g') IS NULL ALTER TABLE [FoodItems] ADD [CarbsPer100g] decimal(18,2) NOT NULL CONSTRAINT [DF_FoodItems_Carbs_Repair] DEFAULT 0;
    IF COL_LENGTH(N'dbo.FoodItems', N'ServingSize') IS NULL ALTER TABLE [FoodItems] ADD [ServingSize] nvarchar(100) NULL;
    IF COL_LENGTH(N'dbo.FoodItems', N'CreatedAt') IS NULL ALTER TABLE [FoodItems] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_FoodItems_CreatedAt_Repair] DEFAULT SYSUTCDATETIME();
END;
IF OBJECT_ID(N'dbo.FoodItems', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.FoodItems') AND [type] = 'PK')
        ALTER TABLE [FoodItems] ADD CONSTRAINT [PK_FoodItems] PRIMARY KEY ([Id]);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FoodItems_Name' AND object_id = OBJECT_ID(N'dbo.FoodItems'))
        CREATE INDEX [IX_FoodItems_Name] ON [FoodItems] ([Name]);
END;

IF OBJECT_ID(N'dbo.MealLogs', N'U') IS NULL
BEGIN
    CREATE TABLE [MealLogs] (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_MealLogs_Id_Repair] DEFAULT NEWID(),
        [UserId] uniqueidentifier NOT NULL,
        [FoodId] uniqueidentifier NOT NULL,
        [MealImageUrl] nvarchar(max) NULL,
        [DetectedAt] datetime2 NULL,
        [QuantityGrams] decimal(18,2) NULL,
        [CalculatedCalories] decimal(18,2) NULL,
        [CalculatedProtein] decimal(18,2) NULL,
        [CalculatedFat] decimal(18,2) NULL,
        [CalculatedCarbs] decimal(18,2) NULL,
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_MealLogs_CreatedAt_Repair] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_MealLogs] PRIMARY KEY ([Id])
    );
END
ELSE
BEGIN
    IF COL_LENGTH(N'dbo.MealLogs', N'Id') IS NULL ALTER TABLE [MealLogs] ADD [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_MealLogs_Id_Repair] DEFAULT NEWID();
    IF COL_LENGTH(N'dbo.MealLogs', N'UserId') IS NULL ALTER TABLE [MealLogs] ADD [UserId] uniqueidentifier NOT NULL CONSTRAINT [DF_MealLogs_UserId_Repair] DEFAULT '00000000-0000-0000-0000-000000000000';
    IF COL_LENGTH(N'dbo.MealLogs', N'FoodId') IS NULL ALTER TABLE [MealLogs] ADD [FoodId] uniqueidentifier NOT NULL CONSTRAINT [DF_MealLogs_FoodId_Repair] DEFAULT '00000000-0000-0000-0000-000000000000';
    IF COL_LENGTH(N'dbo.MealLogs', N'MealImageUrl') IS NULL ALTER TABLE [MealLogs] ADD [MealImageUrl] nvarchar(max) NULL;
    IF COL_LENGTH(N'dbo.MealLogs', N'DetectedAt') IS NULL ALTER TABLE [MealLogs] ADD [DetectedAt] datetime2 NULL;
    IF COL_LENGTH(N'dbo.MealLogs', N'QuantityGrams') IS NULL ALTER TABLE [MealLogs] ADD [QuantityGrams] decimal(18,2) NULL;
    IF COL_LENGTH(N'dbo.MealLogs', N'CalculatedCalories') IS NULL ALTER TABLE [MealLogs] ADD [CalculatedCalories] decimal(18,2) NULL;
    IF COL_LENGTH(N'dbo.MealLogs', N'CalculatedProtein') IS NULL ALTER TABLE [MealLogs] ADD [CalculatedProtein] decimal(18,2) NULL;
    IF COL_LENGTH(N'dbo.MealLogs', N'CalculatedFat') IS NULL ALTER TABLE [MealLogs] ADD [CalculatedFat] decimal(18,2) NULL;
    IF COL_LENGTH(N'dbo.MealLogs', N'CalculatedCarbs') IS NULL ALTER TABLE [MealLogs] ADD [CalculatedCarbs] decimal(18,2) NULL;
    IF COL_LENGTH(N'dbo.MealLogs', N'CreatedAt') IS NULL ALTER TABLE [MealLogs] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_MealLogs_CreatedAt_Repair] DEFAULT SYSUTCDATETIME();
END;
IF OBJECT_ID(N'dbo.MealLogs', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.MealLogs') AND [type] = 'PK')
        ALTER TABLE [MealLogs] ADD CONSTRAINT [PK_MealLogs] PRIMARY KEY ([Id]);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MealLogs_UserId_CreatedAt' AND object_id = OBJECT_ID(N'dbo.MealLogs'))
        CREATE INDEX [IX_MealLogs_UserId_CreatedAt] ON [MealLogs] ([UserId], [CreatedAt]);
END;

/* Decimal precision repair for old databases */
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Users', N'HeightCm') IS NOT NULL ALTER TABLE [Users] ALTER COLUMN [HeightCm] decimal(18,2) NULL;
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Users', N'WeightKg') IS NOT NULL ALTER TABLE [Users] ALTER COLUMN [WeightKg] decimal(18,2) NULL;
IF OBJECT_ID(N'dbo.FoodItems', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.FoodItems', N'CaloriesPer100g') IS NOT NULL ALTER TABLE [FoodItems] ALTER COLUMN [CaloriesPer100g] decimal(18,2) NOT NULL;
IF OBJECT_ID(N'dbo.FoodItems', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.FoodItems', N'ProteinPer100g') IS NOT NULL ALTER TABLE [FoodItems] ALTER COLUMN [ProteinPer100g] decimal(18,2) NOT NULL;
IF OBJECT_ID(N'dbo.FoodItems', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.FoodItems', N'FatPer100g') IS NOT NULL ALTER TABLE [FoodItems] ALTER COLUMN [FatPer100g] decimal(18,2) NOT NULL;
IF OBJECT_ID(N'dbo.FoodItems', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.FoodItems', N'CarbsPer100g') IS NOT NULL ALTER TABLE [FoodItems] ALTER COLUMN [CarbsPer100g] decimal(18,2) NOT NULL;
IF OBJECT_ID(N'dbo.MealLogs', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.MealLogs', N'QuantityGrams') IS NOT NULL ALTER TABLE [MealLogs] ALTER COLUMN [QuantityGrams] decimal(18,2) NULL;
IF OBJECT_ID(N'dbo.MealLogs', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.MealLogs', N'CalculatedCalories') IS NOT NULL ALTER TABLE [MealLogs] ALTER COLUMN [CalculatedCalories] decimal(18,2) NULL;
IF OBJECT_ID(N'dbo.MealLogs', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.MealLogs', N'CalculatedProtein') IS NOT NULL ALTER TABLE [MealLogs] ALTER COLUMN [CalculatedProtein] decimal(18,2) NULL;
IF OBJECT_ID(N'dbo.MealLogs', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.MealLogs', N'CalculatedFat') IS NOT NULL ALTER TABLE [MealLogs] ALTER COLUMN [CalculatedFat] decimal(18,2) NULL;
IF OBJECT_ID(N'dbo.MealLogs', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.MealLogs', N'CalculatedCarbs') IS NOT NULL ALTER TABLE [MealLogs] ALTER COLUMN [CalculatedCarbs] decimal(18,2) NULL;

/* Community */
IF OBJECT_ID(N'dbo.CommunityPosts', N'U') IS NULL
BEGIN
    CREATE TABLE [CommunityPosts] (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_CommunityPosts_Id_Repair] DEFAULT NEWID(),
        [UserId] uniqueidentifier NOT NULL,
        [Content] nvarchar(max) NOT NULL CONSTRAINT [DF_CommunityPosts_Content_Repair] DEFAULT N'',
        [ImageUrl] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_CommunityPosts_CreatedAt_Repair] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_CommunityPosts] PRIMARY KEY ([Id])
    );
END
ELSE
BEGIN
    IF COL_LENGTH(N'dbo.CommunityPosts', N'Id') IS NULL ALTER TABLE [CommunityPosts] ADD [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_CommunityPosts_Id_Repair] DEFAULT NEWID();
    IF COL_LENGTH(N'dbo.CommunityPosts', N'UserId') IS NULL ALTER TABLE [CommunityPosts] ADD [UserId] uniqueidentifier NOT NULL CONSTRAINT [DF_CommunityPosts_UserId_Repair] DEFAULT '00000000-0000-0000-0000-000000000000';
    IF COL_LENGTH(N'dbo.CommunityPosts', N'Content') IS NULL ALTER TABLE [CommunityPosts] ADD [Content] nvarchar(max) NOT NULL CONSTRAINT [DF_CommunityPosts_Content_Repair] DEFAULT N'';
    IF COL_LENGTH(N'dbo.CommunityPosts', N'ImageUrl') IS NULL ALTER TABLE [CommunityPosts] ADD [ImageUrl] nvarchar(max) NULL;
    IF COL_LENGTH(N'dbo.CommunityPosts', N'CreatedAt') IS NULL ALTER TABLE [CommunityPosts] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_CommunityPosts_CreatedAt_Repair] DEFAULT SYSUTCDATETIME();
END;
IF OBJECT_ID(N'dbo.CommunityPosts', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.CommunityPosts') AND [type] = 'PK')
        ALTER TABLE [CommunityPosts] ADD CONSTRAINT [PK_CommunityPosts] PRIMARY KEY ([Id]);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CommunityPosts_UserId_CreatedAt' AND object_id = OBJECT_ID(N'dbo.CommunityPosts'))
        CREATE INDEX [IX_CommunityPosts_UserId_CreatedAt] ON [CommunityPosts] ([UserId], [CreatedAt]);
END;

IF OBJECT_ID(N'dbo.Comments', N'U') IS NULL
BEGIN
    CREATE TABLE [Comments] (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_Comments_Id_Repair] DEFAULT NEWID(),
        [PostId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Text] nvarchar(max) NOT NULL CONSTRAINT [DF_Comments_Text_Repair] DEFAULT N'',
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_Comments_CreatedAt_Repair] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_Comments] PRIMARY KEY ([Id])
    );
END
ELSE
BEGIN
    IF COL_LENGTH(N'dbo.Comments', N'Id') IS NULL ALTER TABLE [Comments] ADD [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_Comments_Id_Repair] DEFAULT NEWID();
    IF COL_LENGTH(N'dbo.Comments', N'PostId') IS NULL ALTER TABLE [Comments] ADD [PostId] uniqueidentifier NOT NULL CONSTRAINT [DF_Comments_PostId_Repair] DEFAULT '00000000-0000-0000-0000-000000000000';
    IF COL_LENGTH(N'dbo.Comments', N'UserId') IS NULL ALTER TABLE [Comments] ADD [UserId] uniqueidentifier NOT NULL CONSTRAINT [DF_Comments_UserId_Repair] DEFAULT '00000000-0000-0000-0000-000000000000';
    IF COL_LENGTH(N'dbo.Comments', N'Text') IS NULL ALTER TABLE [Comments] ADD [Text] nvarchar(max) NOT NULL CONSTRAINT [DF_Comments_Text_Repair] DEFAULT N'';
    IF COL_LENGTH(N'dbo.Comments', N'CreatedAt') IS NULL ALTER TABLE [Comments] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_Comments_CreatedAt_Repair] DEFAULT SYSUTCDATETIME();
END;
IF OBJECT_ID(N'dbo.Comments', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.Comments') AND [type] = 'PK')
        ALTER TABLE [Comments] ADD CONSTRAINT [PK_Comments] PRIMARY KEY ([Id]);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Comments_PostId_CreatedAt' AND object_id = OBJECT_ID(N'dbo.Comments'))
        CREATE INDEX [IX_Comments_PostId_CreatedAt] ON [Comments] ([PostId], [CreatedAt]);
END;

IF OBJECT_ID(N'dbo.PostLikes', N'U') IS NULL
BEGIN
    CREATE TABLE [PostLikes] (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_PostLikes_Id_Repair] DEFAULT NEWID(),
        [PostId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_PostLikes_CreatedAt_Repair] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_PostLikes] PRIMARY KEY ([Id])
    );
END
ELSE
BEGIN
    IF COL_LENGTH(N'dbo.PostLikes', N'Id') IS NULL ALTER TABLE [PostLikes] ADD [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_PostLikes_Id_Repair] DEFAULT NEWID();
    IF COL_LENGTH(N'dbo.PostLikes', N'PostId') IS NULL ALTER TABLE [PostLikes] ADD [PostId] uniqueidentifier NOT NULL CONSTRAINT [DF_PostLikes_PostId_Repair] DEFAULT '00000000-0000-0000-0000-000000000000';
    IF COL_LENGTH(N'dbo.PostLikes', N'UserId') IS NULL ALTER TABLE [PostLikes] ADD [UserId] uniqueidentifier NOT NULL CONSTRAINT [DF_PostLikes_UserId_Repair] DEFAULT '00000000-0000-0000-0000-000000000000';
    IF COL_LENGTH(N'dbo.PostLikes', N'CreatedAt') IS NULL ALTER TABLE [PostLikes] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_PostLikes_CreatedAt_Repair] DEFAULT SYSUTCDATETIME();
END;
IF OBJECT_ID(N'dbo.PostLikes', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.PostLikes') AND [type] = 'PK')
        ALTER TABLE [PostLikes] ADD CONSTRAINT [PK_PostLikes] PRIMARY KEY ([Id]);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PostLikes_PostId_UserId' AND object_id = OBJECT_ID(N'dbo.PostLikes'))
        CREATE INDEX [IX_PostLikes_PostId_UserId] ON [PostLikes] ([PostId], [UserId]);
END;



/* Real followers / following */
IF OBJECT_ID(N'dbo.UserFollows', N'U') IS NULL
BEGIN
    CREATE TABLE [UserFollows] (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_UserFollows_Id_Repair] DEFAULT NEWID(),
        [FollowerId] uniqueidentifier NOT NULL,
        [FollowingId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_UserFollows_CreatedAt_Repair] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_UserFollows] PRIMARY KEY ([Id])
    );
END
ELSE
BEGIN
    IF COL_LENGTH(N'dbo.UserFollows', N'Id') IS NULL ALTER TABLE [UserFollows] ADD [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_UserFollows_Id_Repair] DEFAULT NEWID();
    IF COL_LENGTH(N'dbo.UserFollows', N'FollowerId') IS NULL ALTER TABLE [UserFollows] ADD [FollowerId] uniqueidentifier NOT NULL CONSTRAINT [DF_UserFollows_FollowerId_Repair] DEFAULT '00000000-0000-0000-0000-000000000000';
    IF COL_LENGTH(N'dbo.UserFollows', N'FollowingId') IS NULL ALTER TABLE [UserFollows] ADD [FollowingId] uniqueidentifier NOT NULL CONSTRAINT [DF_UserFollows_FollowingId_Repair] DEFAULT '00000000-0000-0000-0000-000000000000';
    IF COL_LENGTH(N'dbo.UserFollows', N'CreatedAt') IS NULL ALTER TABLE [UserFollows] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_UserFollows_CreatedAt_Repair] DEFAULT SYSUTCDATETIME();
END;
IF OBJECT_ID(N'dbo.UserFollows', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.UserFollows') AND [type] = 'PK')
        ALTER TABLE [UserFollows] ADD CONSTRAINT [PK_UserFollows] PRIMARY KEY ([Id]);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UserFollows_FollowerId_FollowingId' AND object_id = OBJECT_ID(N'dbo.UserFollows'))
        CREATE UNIQUE INDEX [IX_UserFollows_FollowerId_FollowingId] ON [UserFollows] ([FollowerId], [FollowingId]);
END;
/* Chat: create missing tables, then repair media / voice-note columns */
IF OBJECT_ID(N'dbo.ChatThreads', N'U') IS NULL
BEGIN
    CREATE TABLE [ChatThreads] (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_ChatThreads_Id_Repair] DEFAULT NEWID(),
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_ChatThreads_CreatedAt_Repair] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_ChatThreads] PRIMARY KEY ([Id])
    );
END
ELSE
BEGIN
    IF COL_LENGTH(N'dbo.ChatThreads', N'Id') IS NULL ALTER TABLE [ChatThreads] ADD [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_ChatThreads_Id_Repair] DEFAULT NEWID();
    IF COL_LENGTH(N'dbo.ChatThreads', N'CreatedAt') IS NULL ALTER TABLE [ChatThreads] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_ChatThreads_CreatedAt_Repair] DEFAULT SYSUTCDATETIME();
END;

IF OBJECT_ID(N'dbo.ChatParticipants', N'U') IS NULL
BEGIN
    CREATE TABLE [ChatParticipants] (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_ChatParticipants_Id_Repair] DEFAULT NEWID(),
        [ThreadId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [JoinedAt] datetime2 NOT NULL CONSTRAINT [DF_ChatParticipants_JoinedAt_Repair] DEFAULT SYSUTCDATETIME(),
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_ChatParticipants_CreatedAt_Repair] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_ChatParticipants] PRIMARY KEY ([Id])
    );
END
ELSE
BEGIN
    IF COL_LENGTH(N'dbo.ChatParticipants', N'Id') IS NULL ALTER TABLE [ChatParticipants] ADD [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_ChatParticipants_Id_Repair] DEFAULT NEWID();
    IF COL_LENGTH(N'dbo.ChatParticipants', N'ThreadId') IS NULL ALTER TABLE [ChatParticipants] ADD [ThreadId] uniqueidentifier NOT NULL CONSTRAINT [DF_ChatParticipants_ThreadId_Repair] DEFAULT '00000000-0000-0000-0000-000000000000';
    IF COL_LENGTH(N'dbo.ChatParticipants', N'UserId') IS NULL ALTER TABLE [ChatParticipants] ADD [UserId] uniqueidentifier NOT NULL CONSTRAINT [DF_ChatParticipants_UserId_Repair] DEFAULT '00000000-0000-0000-0000-000000000000';
    IF COL_LENGTH(N'dbo.ChatParticipants', N'JoinedAt') IS NULL ALTER TABLE [ChatParticipants] ADD [JoinedAt] datetime2 NOT NULL CONSTRAINT [DF_ChatParticipants_JoinedAt_Repair] DEFAULT SYSUTCDATETIME();
    IF COL_LENGTH(N'dbo.ChatParticipants', N'CreatedAt') IS NULL ALTER TABLE [ChatParticipants] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_ChatParticipants_CreatedAt_Repair] DEFAULT SYSUTCDATETIME();
END;
IF OBJECT_ID(N'dbo.ChatParticipants', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ChatParticipants_ThreadId_UserId' AND object_id = OBJECT_ID(N'dbo.ChatParticipants'))
        CREATE UNIQUE INDEX [IX_ChatParticipants_ThreadId_UserId] ON [ChatParticipants] ([ThreadId], [UserId]);
END;

IF OBJECT_ID(N'dbo.ChatMessages', N'U') IS NULL
BEGIN
    CREATE TABLE [ChatMessages] (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_ChatMessages_Id_Repair] DEFAULT NEWID(),
        [ThreadId] uniqueidentifier NOT NULL,
        [SenderId] uniqueidentifier NOT NULL,
        [MessageText] nvarchar(max) NOT NULL CONSTRAINT [DF_ChatMessages_MessageText_Repair] DEFAULT N'',
        [MessageType] nvarchar(30) NOT NULL CONSTRAINT [DF_ChatMessages_MessageType_Repair] DEFAULT N'text',
        [MediaContent] nvarchar(max) NULL,
        [FileName] nvarchar(255) NULL,
        [SentAt] datetime2 NOT NULL CONSTRAINT [DF_ChatMessages_SentAt_Repair] DEFAULT SYSUTCDATETIME(),
        [IsDeleted] bit NOT NULL CONSTRAINT [DF_ChatMessages_IsDeleted_Repair] DEFAULT CAST(0 AS bit),
        [EditedAt] datetime2 NULL,
        [DeletedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_ChatMessages_CreatedAt_Repair] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_ChatMessages] PRIMARY KEY ([Id])
    );
END
ELSE
BEGIN
    IF COL_LENGTH(N'dbo.ChatMessages', N'Id') IS NULL ALTER TABLE [ChatMessages] ADD [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_ChatMessages_Id_Repair] DEFAULT NEWID();
    IF COL_LENGTH(N'dbo.ChatMessages', N'ThreadId') IS NULL ALTER TABLE [ChatMessages] ADD [ThreadId] uniqueidentifier NOT NULL CONSTRAINT [DF_ChatMessages_ThreadId_Repair] DEFAULT '00000000-0000-0000-0000-000000000000';
    IF COL_LENGTH(N'dbo.ChatMessages', N'SenderId') IS NULL ALTER TABLE [ChatMessages] ADD [SenderId] uniqueidentifier NOT NULL CONSTRAINT [DF_ChatMessages_SenderId_Repair] DEFAULT '00000000-0000-0000-0000-000000000000';
    IF COL_LENGTH(N'dbo.ChatMessages', N'MessageText') IS NULL ALTER TABLE [ChatMessages] ADD [MessageText] nvarchar(max) NOT NULL CONSTRAINT [DF_ChatMessages_MessageText_Repair] DEFAULT N'';
    IF COL_LENGTH(N'dbo.ChatMessages', N'MessageType') IS NULL ALTER TABLE [ChatMessages] ADD [MessageType] nvarchar(30) NOT NULL CONSTRAINT [DF_ChatMessages_MessageType_Repair] DEFAULT N'text';
    IF COL_LENGTH(N'dbo.ChatMessages', N'MediaContent') IS NULL ALTER TABLE [ChatMessages] ADD [MediaContent] nvarchar(max) NULL;
    IF COL_LENGTH(N'dbo.ChatMessages', N'FileName') IS NULL ALTER TABLE [ChatMessages] ADD [FileName] nvarchar(255) NULL;
    IF COL_LENGTH(N'dbo.ChatMessages', N'SentAt') IS NULL ALTER TABLE [ChatMessages] ADD [SentAt] datetime2 NOT NULL CONSTRAINT [DF_ChatMessages_SentAt_Repair] DEFAULT SYSUTCDATETIME();
    IF COL_LENGTH(N'dbo.ChatMessages', N'IsDeleted') IS NULL ALTER TABLE [ChatMessages] ADD [IsDeleted] bit NOT NULL CONSTRAINT [DF_ChatMessages_IsDeleted_Repair] DEFAULT CAST(0 AS bit);
    IF COL_LENGTH(N'dbo.ChatMessages', N'EditedAt') IS NULL ALTER TABLE [ChatMessages] ADD [EditedAt] datetime2 NULL;
    IF COL_LENGTH(N'dbo.ChatMessages', N'DeletedAt') IS NULL ALTER TABLE [ChatMessages] ADD [DeletedAt] datetime2 NULL;
    IF COL_LENGTH(N'dbo.ChatMessages', N'CreatedAt') IS NULL ALTER TABLE [ChatMessages] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_ChatMessages_CreatedAt_Repair] DEFAULT SYSUTCDATETIME();
    EXEC(N'UPDATE [ChatMessages] SET [MessageText] = N'''' WHERE [MessageText] IS NULL;');
    EXEC(N'UPDATE [ChatMessages] SET [IsDeleted] = CAST(0 AS bit) WHERE [IsDeleted] IS NULL;');
    EXEC(N'ALTER TABLE [ChatMessages] ALTER COLUMN [MessageText] nvarchar(max) NOT NULL;');
    EXEC(N'ALTER TABLE [ChatMessages] ALTER COLUMN [IsDeleted] bit NOT NULL;');
END;
IF OBJECT_ID(N'dbo.ChatMessages', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ChatMessages', N'IsDeleted') IS NOT NULL
       AND NOT EXISTS (
           SELECT 1
           FROM sys.default_constraints dc
           INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
           WHERE dc.parent_object_id = OBJECT_ID(N'dbo.ChatMessages')
             AND c.name = N'IsDeleted')
    BEGIN
        EXEC(N'ALTER TABLE [ChatMessages] ADD CONSTRAINT [DF_ChatMessages_IsDeleted_Repair] DEFAULT CAST(0 AS bit) FOR [IsDeleted];');
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ChatMessages_ThreadId_SentAt' AND object_id = OBJECT_ID(N'dbo.ChatMessages'))
        CREATE INDEX [IX_ChatMessages_ThreadId_SentAt] ON [ChatMessages] ([ThreadId], [SentAt]);
END;

");
    }
}

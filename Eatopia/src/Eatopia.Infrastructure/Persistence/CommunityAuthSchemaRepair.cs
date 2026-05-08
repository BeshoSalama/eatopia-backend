using Microsoft.EntityFrameworkCore;

namespace Eatopia.Infrastructure.Persistence;

public static class CommunityAuthSchemaRepair
{
    public static void Apply(EatopiaDbContext db)
    {
        // Important: SQL Server compiles a full batch before applying ALTER TABLE.
        // Any command that uses a column which may have just been added is executed
        // as dynamic SQL or in a separate ExecuteSqlRaw call to avoid:
        // Invalid column name 'EmailConfirmationTokenHash'.
        db.Database.ExecuteSqlRaw(@"
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Users', N'LastSeenAt') IS NULL ALTER TABLE [Users] ADD [LastSeenAt] datetime2 NULL;
    IF COL_LENGTH(N'dbo.Users', N'IsBanned') IS NULL ALTER TABLE [Users] ADD [IsBanned] bit NOT NULL CONSTRAINT [DF_Users_IsBanned_Repair] DEFAULT CAST(0 AS bit);
    IF COL_LENGTH(N'dbo.Users', N'BannedAt') IS NULL ALTER TABLE [Users] ADD [BannedAt] datetime2 NULL;
    IF COL_LENGTH(N'dbo.Users', N'BannedReason') IS NULL ALTER TABLE [Users] ADD [BannedReason] nvarchar(1000) NULL;
    IF COL_LENGTH(N'dbo.Users', N'EmailConfirmed') IS NULL ALTER TABLE [Users] ADD [EmailConfirmed] bit NOT NULL CONSTRAINT [DF_Users_EmailConfirmed_Repair] DEFAULT CAST(0 AS bit);
    IF COL_LENGTH(N'dbo.Users', N'EmailConfirmedAt') IS NULL ALTER TABLE [Users] ADD [EmailConfirmedAt] datetime2 NULL;
    IF COL_LENGTH(N'dbo.Users', N'EmailConfirmationTokenHash') IS NULL ALTER TABLE [Users] ADD [EmailConfirmationTokenHash] nvarchar(max) NULL;
    IF COL_LENGTH(N'dbo.Users', N'EmailConfirmationTokenExpiresAt') IS NULL ALTER TABLE [Users] ADD [EmailConfirmationTokenExpiresAt] datetime2 NULL;
    IF COL_LENGTH(N'dbo.Users', N'LastEmailConfirmationSentAt') IS NULL ALTER TABLE [Users] ADD [LastEmailConfirmationSentAt] datetime2 NULL;
END;
");

        db.Database.ExecuteSqlRaw(@"
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Users', N'EmailConfirmed') IS NOT NULL
   AND COL_LENGTH(N'dbo.Users', N'EmailConfirmationTokenHash') IS NOT NULL
   AND COL_LENGTH(N'dbo.Users', N'EmailConfirmedAt') IS NOT NULL
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

        db.Database.ExecuteSqlRaw(@"
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.CommunityPosts', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.CommunityPosts', N'SharedPostId') IS NULL ALTER TABLE [CommunityPosts] ADD [SharedPostId] uniqueidentifier NULL;
    IF COL_LENGTH(N'dbo.CommunityPosts', N'ImageUrl') IS NULL ALTER TABLE [CommunityPosts] ADD [ImageUrl] nvarchar(max) NULL;
    IF COL_LENGTH(N'dbo.CommunityPosts', N'IsDeleted') IS NULL ALTER TABLE [CommunityPosts] ADD [IsDeleted] bit NOT NULL CONSTRAINT [DF_CommunityPosts_IsDeleted_Repair] DEFAULT CAST(0 AS bit);
    IF COL_LENGTH(N'dbo.CommunityPosts', N'DeletedAt') IS NULL ALTER TABLE [CommunityPosts] ADD [DeletedAt] datetime2 NULL;
END;
");

        db.Database.ExecuteSqlRaw(@"
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.CommunityPosts', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.CommunityPosts', N'Content') IS NOT NULL
BEGIN
    EXEC(N'UPDATE [CommunityPosts] SET [Content] = N'''' WHERE [Content] IS NULL;');
    ALTER TABLE [CommunityPosts] ALTER COLUMN [Content] nvarchar(max) NOT NULL;
END;
");

        db.Database.ExecuteSqlRaw(@"
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.PasswordResetCodes', N'U') IS NULL
BEGIN
    CREATE TABLE [PasswordResetCodes] (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_PasswordResetCodes_Id_Repair] DEFAULT NEWID(),
        [UserId] uniqueidentifier NOT NULL,
        [CodeHash] nvarchar(max) NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [UsedAt] datetime2 NULL,
        [IsUsed] bit NOT NULL CONSTRAINT [DF_PasswordResetCodes_IsUsed_Repair] DEFAULT CAST(0 AS bit),
        [AttemptCount] int NOT NULL CONSTRAINT [DF_PasswordResetCodes_AttemptCount_Repair] DEFAULT 0,
        [LastAttemptAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_PasswordResetCodes_CreatedAt_Repair] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_PasswordResetCodes] PRIMARY KEY ([Id])
    );
END
ELSE
BEGIN
    IF COL_LENGTH(N'dbo.PasswordResetCodes', N'Id') IS NULL ALTER TABLE [PasswordResetCodes] ADD [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_PasswordResetCodes_Id_Repair] DEFAULT NEWID();
    IF COL_LENGTH(N'dbo.PasswordResetCodes', N'UserId') IS NULL ALTER TABLE [PasswordResetCodes] ADD [UserId] uniqueidentifier NOT NULL CONSTRAINT [DF_PasswordResetCodes_UserId_Repair] DEFAULT '00000000-0000-0000-0000-000000000000';
    IF COL_LENGTH(N'dbo.PasswordResetCodes', N'CodeHash') IS NULL ALTER TABLE [PasswordResetCodes] ADD [CodeHash] nvarchar(max) NOT NULL CONSTRAINT [DF_PasswordResetCodes_CodeHash_Repair] DEFAULT N'';
    IF COL_LENGTH(N'dbo.PasswordResetCodes', N'ExpiresAt') IS NULL ALTER TABLE [PasswordResetCodes] ADD [ExpiresAt] datetime2 NOT NULL CONSTRAINT [DF_PasswordResetCodes_ExpiresAt_Repair] DEFAULT SYSUTCDATETIME();
    IF COL_LENGTH(N'dbo.PasswordResetCodes', N'UsedAt') IS NULL ALTER TABLE [PasswordResetCodes] ADD [UsedAt] datetime2 NULL;
    IF COL_LENGTH(N'dbo.PasswordResetCodes', N'IsUsed') IS NULL ALTER TABLE [PasswordResetCodes] ADD [IsUsed] bit NOT NULL CONSTRAINT [DF_PasswordResetCodes_IsUsed_Repair] DEFAULT CAST(0 AS bit);
    IF COL_LENGTH(N'dbo.PasswordResetCodes', N'AttemptCount') IS NULL ALTER TABLE [PasswordResetCodes] ADD [AttemptCount] int NOT NULL CONSTRAINT [DF_PasswordResetCodes_AttemptCount_Repair] DEFAULT 0;
    IF COL_LENGTH(N'dbo.PasswordResetCodes', N'LastAttemptAt') IS NULL ALTER TABLE [PasswordResetCodes] ADD [LastAttemptAt] datetime2 NULL;
    IF COL_LENGTH(N'dbo.PasswordResetCodes', N'CreatedAt') IS NULL ALTER TABLE [PasswordResetCodes] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_PasswordResetCodes_CreatedAt_Repair] DEFAULT SYSUTCDATETIME();
END;
");

        db.Database.ExecuteSqlRaw(@"
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.PasswordResetCodes', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PasswordResetCodes_UserId_ExpiresAt' AND object_id = OBJECT_ID(N'dbo.PasswordResetCodes'))
BEGIN
    EXEC(N'CREATE INDEX [IX_PasswordResetCodes_UserId_ExpiresAt] ON [PasswordResetCodes] ([UserId], [ExpiresAt]);');
END;

IF OBJECT_ID(N'dbo.CommunityPosts', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.CommunityPosts', N'IsDeleted') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CommunityPosts_IsDeleted' AND object_id = OBJECT_ID(N'dbo.CommunityPosts'))
BEGIN
    EXEC(N'CREATE INDEX [IX_CommunityPosts_IsDeleted] ON [CommunityPosts] ([IsDeleted]);');
END;
");
        db.Database.ExecuteSqlRaw(@"
SET NOCOUNT ON;

/* Stabilization columns */
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Users', N'FailedLoginAttemptCount') IS NULL ALTER TABLE [Users] ADD [FailedLoginAttemptCount] int NOT NULL CONSTRAINT [DF_Users_FailedLoginAttemptCount_Repair] DEFAULT 0;
    IF COL_LENGTH(N'dbo.Users', N'LoginLockoutEndAt') IS NULL ALTER TABLE [Users] ADD [LoginLockoutEndAt] datetime2 NULL;
    IF COL_LENGTH(N'dbo.Users', N'JwtTokenVersion') IS NULL ALTER TABLE [Users] ADD [JwtTokenVersion] int NOT NULL CONSTRAINT [DF_Users_JwtTokenVersion_Repair] DEFAULT 0;
    IF COL_LENGTH(N'dbo.Users', N'IsBanned') IS NULL ALTER TABLE [Users] ADD [IsBanned] bit NOT NULL CONSTRAINT [DF_Users_IsBanned_Repair] DEFAULT CAST(0 AS bit);
    IF COL_LENGTH(N'dbo.Users', N'BannedAt') IS NULL ALTER TABLE [Users] ADD [BannedAt] datetime2 NULL;
    IF COL_LENGTH(N'dbo.Users', N'BannedReason') IS NULL ALTER TABLE [Users] ADD [BannedReason] nvarchar(1000) NULL;
    IF COL_LENGTH(N'dbo.Users', N'NotificationsEnabled') IS NULL ALTER TABLE [Users] ADD [NotificationsEnabled] bit NOT NULL CONSTRAINT [DF_Users_NotificationsEnabled_Repair] DEFAULT CAST(1 AS bit);
    IF COL_LENGTH(N'dbo.Users', N'MessageNotificationsEnabled') IS NULL ALTER TABLE [Users] ADD [MessageNotificationsEnabled] bit NOT NULL CONSTRAINT [DF_Users_MessageNotificationsEnabled_Repair] DEFAULT CAST(1 AS bit);
    IF COL_LENGTH(N'dbo.Users', N'CommunityNotificationsEnabled') IS NULL ALTER TABLE [Users] ADD [CommunityNotificationsEnabled] bit NOT NULL CONSTRAINT [DF_Users_CommunityNotificationsEnabled_Repair] DEFAULT CAST(1 AS bit);
    IF COL_LENGTH(N'dbo.Users', N'EmailNotificationsEnabled') IS NULL ALTER TABLE [Users] ADD [EmailNotificationsEnabled] bit NOT NULL CONSTRAINT [DF_Users_EmailNotificationsEnabled_Repair] DEFAULT CAST(1 AS bit);
    IF COL_LENGTH(N'dbo.Users', N'ProfileVisibility') IS NULL ALTER TABLE [Users] ADD [ProfileVisibility] nvarchar(20) NOT NULL CONSTRAINT [DF_Users_ProfileVisibility_Repair] DEFAULT N'Public';
    IF COL_LENGTH(N'dbo.Users', N'PostsVisibility') IS NULL ALTER TABLE [Users] ADD [PostsVisibility] nvarchar(20) NOT NULL CONSTRAINT [DF_Users_PostsVisibility_Repair] DEFAULT N'Public';
    IF COL_LENGTH(N'dbo.Users', N'ShowOnlineStatus') IS NULL ALTER TABLE [Users] ADD [ShowOnlineStatus] bit NOT NULL CONSTRAINT [DF_Users_ShowOnlineStatus_Repair] DEFAULT CAST(1 AS bit);
    IF COL_LENGTH(N'dbo.Users', N'ShowLastSeen') IS NULL ALTER TABLE [Users] ADD [ShowLastSeen] bit NOT NULL CONSTRAINT [DF_Users_ShowLastSeen_Repair] DEFAULT CAST(1 AS bit);
    IF COL_LENGTH(N'dbo.Users', N'AllowMessageRequests') IS NULL ALTER TABLE [Users] ADD [AllowMessageRequests] bit NOT NULL CONSTRAINT [DF_Users_AllowMessageRequests_Repair] DEFAULT CAST(1 AS bit);
    IF COL_LENGTH(N'dbo.Users', N'AllowSearchByEmail') IS NULL ALTER TABLE [Users] ADD [AllowSearchByEmail] bit NOT NULL CONSTRAINT [DF_Users_AllowSearchByEmail_Repair] DEFAULT CAST(1 AS bit);
END;

IF OBJECT_ID(N'dbo.ChatThreads', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ChatThreads', N'RequestStatus') IS NULL ALTER TABLE [ChatThreads] ADD [RequestStatus] nvarchar(30) NOT NULL CONSTRAINT [DF_ChatThreads_RequestStatus_Repair] DEFAULT N'Pending';
    IF COL_LENGTH(N'dbo.ChatThreads', N'RequestedByUserId') IS NULL ALTER TABLE [ChatThreads] ADD [RequestedByUserId] uniqueidentifier NULL;
    IF COL_LENGTH(N'dbo.ChatThreads', N'AcceptedAt') IS NULL ALTER TABLE [ChatThreads] ADD [AcceptedAt] datetime2 NULL;
    IF COL_LENGTH(N'dbo.ChatThreads', N'DeletedAt') IS NULL ALTER TABLE [ChatThreads] ADD [DeletedAt] datetime2 NULL;
END;

IF OBJECT_ID(N'dbo.ChatMessages', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ChatMessages', N'IsDeleted') IS NULL ALTER TABLE [ChatMessages] ADD [IsDeleted] bit NOT NULL CONSTRAINT [DF_ChatMessages_IsDeleted_Repair] DEFAULT CAST(0 AS bit);
    IF COL_LENGTH(N'dbo.ChatMessages', N'DeliveredAt') IS NULL ALTER TABLE [ChatMessages] ADD [DeliveredAt] datetime2 NULL;
    IF COL_LENGTH(N'dbo.ChatMessages', N'SeenAt') IS NULL ALTER TABLE [ChatMessages] ADD [SeenAt] datetime2 NULL;
END;

IF OBJECT_ID(N'dbo.RefreshTokens', N'U') IS NULL
BEGIN
    CREATE TABLE [RefreshTokens] (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_RefreshTokens_Id_Repair] DEFAULT NEWID(),
        [UserId] uniqueidentifier NOT NULL,
        [TokenHash] nvarchar(max) NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [RevokedAt] datetime2 NULL,
        [ReplacedByTokenHash] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_RefreshTokens_CreatedAt_Repair] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id])
    );
END
ELSE
BEGIN
    IF COL_LENGTH(N'dbo.RefreshTokens', N'Id') IS NULL ALTER TABLE [RefreshTokens] ADD [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_RefreshTokens_Id_Repair] DEFAULT NEWID();
    IF COL_LENGTH(N'dbo.RefreshTokens', N'UserId') IS NULL ALTER TABLE [RefreshTokens] ADD [UserId] uniqueidentifier NOT NULL CONSTRAINT [DF_RefreshTokens_UserId_Repair] DEFAULT '00000000-0000-0000-0000-000000000000';
    IF COL_LENGTH(N'dbo.RefreshTokens', N'TokenHash') IS NULL ALTER TABLE [RefreshTokens] ADD [TokenHash] nvarchar(max) NOT NULL CONSTRAINT [DF_RefreshTokens_TokenHash_Repair] DEFAULT N'';
    IF COL_LENGTH(N'dbo.RefreshTokens', N'ExpiresAt') IS NULL ALTER TABLE [RefreshTokens] ADD [ExpiresAt] datetime2 NOT NULL CONSTRAINT [DF_RefreshTokens_ExpiresAt_Repair] DEFAULT SYSUTCDATETIME();
    IF COL_LENGTH(N'dbo.RefreshTokens', N'RevokedAt') IS NULL ALTER TABLE [RefreshTokens] ADD [RevokedAt] datetime2 NULL;
    IF COL_LENGTH(N'dbo.RefreshTokens', N'ReplacedByTokenHash') IS NULL ALTER TABLE [RefreshTokens] ADD [ReplacedByTokenHash] nvarchar(max) NULL;
    IF COL_LENGTH(N'dbo.RefreshTokens', N'CreatedAt') IS NULL ALTER TABLE [RefreshTokens] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_RefreshTokens_CreatedAt_Repair] DEFAULT SYSUTCDATETIME();
END;

IF OBJECT_ID(N'dbo.UserBlocks', N'U') IS NULL
BEGIN
    CREATE TABLE [UserBlocks] (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_UserBlocks_Id_Repair] DEFAULT NEWID(),
        [BlockerId] uniqueidentifier NOT NULL,
        [BlockedId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_UserBlocks_CreatedAt_Repair] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_UserBlocks] PRIMARY KEY ([Id])
    );
END
ELSE
BEGIN
    IF COL_LENGTH(N'dbo.UserBlocks', N'Id') IS NULL ALTER TABLE [UserBlocks] ADD [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_UserBlocks_Id_Repair] DEFAULT NEWID();
    IF COL_LENGTH(N'dbo.UserBlocks', N'BlockerId') IS NULL ALTER TABLE [UserBlocks] ADD [BlockerId] uniqueidentifier NOT NULL CONSTRAINT [DF_UserBlocks_BlockerId_Repair] DEFAULT '00000000-0000-0000-0000-000000000000';
    IF COL_LENGTH(N'dbo.UserBlocks', N'BlockedId') IS NULL ALTER TABLE [UserBlocks] ADD [BlockedId] uniqueidentifier NOT NULL CONSTRAINT [DF_UserBlocks_BlockedId_Repair] DEFAULT '00000000-0000-0000-0000-000000000000';
    IF COL_LENGTH(N'dbo.UserBlocks', N'CreatedAt') IS NULL ALTER TABLE [UserBlocks] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_UserBlocks_CreatedAt_Repair] DEFAULT SYSUTCDATETIME();
END;

IF OBJECT_ID(N'dbo.ContentReports', N'U') IS NULL
BEGIN
    CREATE TABLE [ContentReports] (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_ContentReports_Id_Repair] DEFAULT NEWID(),
        [ReporterId] uniqueidentifier NOT NULL,
        [ContentType] nvarchar(30) NOT NULL,
        [ContentId] uniqueidentifier NOT NULL,
        [ReportedUserId] uniqueidentifier NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [Status] nvarchar(30) NOT NULL CONSTRAINT [DF_ContentReports_Status_Repair] DEFAULT N'Pending',
        [ReviewedAt] datetime2 NULL,
        [ReviewedByUserId] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_ContentReports_CreatedAt_Repair] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_ContentReports] PRIMARY KEY ([Id])
    );
END
ELSE
BEGIN
    IF COL_LENGTH(N'dbo.ContentReports', N'Id') IS NULL ALTER TABLE [ContentReports] ADD [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_ContentReports_Id_Repair] DEFAULT NEWID();
    IF COL_LENGTH(N'dbo.ContentReports', N'ReporterId') IS NULL ALTER TABLE [ContentReports] ADD [ReporterId] uniqueidentifier NOT NULL CONSTRAINT [DF_ContentReports_ReporterId_Repair] DEFAULT '00000000-0000-0000-0000-000000000000';
    IF COL_LENGTH(N'dbo.ContentReports', N'ContentType') IS NULL ALTER TABLE [ContentReports] ADD [ContentType] nvarchar(30) NOT NULL CONSTRAINT [DF_ContentReports_ContentType_Repair] DEFAULT N'Post';
    IF COL_LENGTH(N'dbo.ContentReports', N'ContentId') IS NULL ALTER TABLE [ContentReports] ADD [ContentId] uniqueidentifier NOT NULL CONSTRAINT [DF_ContentReports_ContentId_Repair] DEFAULT '00000000-0000-0000-0000-000000000000';
    IF COL_LENGTH(N'dbo.ContentReports', N'ReportedUserId') IS NULL ALTER TABLE [ContentReports] ADD [ReportedUserId] uniqueidentifier NULL;
    IF COL_LENGTH(N'dbo.ContentReports', N'Reason') IS NULL ALTER TABLE [ContentReports] ADD [Reason] nvarchar(1000) NOT NULL CONSTRAINT [DF_ContentReports_Reason_Repair] DEFAULT N'';
    IF COL_LENGTH(N'dbo.ContentReports', N'Status') IS NULL ALTER TABLE [ContentReports] ADD [Status] nvarchar(30) NOT NULL CONSTRAINT [DF_ContentReports_Status_Repair] DEFAULT N'Pending';
    IF COL_LENGTH(N'dbo.ContentReports', N'ReviewedAt') IS NULL ALTER TABLE [ContentReports] ADD [ReviewedAt] datetime2 NULL;
    IF COL_LENGTH(N'dbo.ContentReports', N'ReviewedByUserId') IS NULL ALTER TABLE [ContentReports] ADD [ReviewedByUserId] uniqueidentifier NULL;
    IF COL_LENGTH(N'dbo.ContentReports', N'CreatedAt') IS NULL ALTER TABLE [ContentReports] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_ContentReports_CreatedAt_Repair] DEFAULT SYSUTCDATETIME();
END;

IF OBJECT_ID(N'dbo.HiddenPosts', N'U') IS NULL
BEGIN
    CREATE TABLE [HiddenPosts] (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_HiddenPosts_Id_Repair] DEFAULT NEWID(),
        [UserId] uniqueidentifier NOT NULL,
        [PostId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_HiddenPosts_CreatedAt_Repair] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_HiddenPosts] PRIMARY KEY ([Id])
    );
END
ELSE
BEGIN
    IF COL_LENGTH(N'dbo.HiddenPosts', N'Id') IS NULL ALTER TABLE [HiddenPosts] ADD [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_HiddenPosts_Id_Repair] DEFAULT NEWID();
    IF COL_LENGTH(N'dbo.HiddenPosts', N'UserId') IS NULL ALTER TABLE [HiddenPosts] ADD [UserId] uniqueidentifier NOT NULL CONSTRAINT [DF_HiddenPosts_UserId_Repair] DEFAULT '00000000-0000-0000-0000-000000000000';
    IF COL_LENGTH(N'dbo.HiddenPosts', N'PostId') IS NULL ALTER TABLE [HiddenPosts] ADD [PostId] uniqueidentifier NOT NULL CONSTRAINT [DF_HiddenPosts_PostId_Repair] DEFAULT '00000000-0000-0000-0000-000000000000';
    IF COL_LENGTH(N'dbo.HiddenPosts', N'CreatedAt') IS NULL ALTER TABLE [HiddenPosts] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_HiddenPosts_CreatedAt_Repair] DEFAULT SYSUTCDATETIME();
END;

IF OBJECT_ID(N'dbo.RefreshTokens', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RefreshTokens_UserId_ExpiresAt' AND object_id = OBJECT_ID(N'dbo.RefreshTokens'))
    EXEC(N'CREATE INDEX [IX_RefreshTokens_UserId_ExpiresAt] ON [RefreshTokens] ([UserId], [ExpiresAt]);');

IF OBJECT_ID(N'dbo.UserBlocks', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UserBlocks_BlockerId_BlockedId' AND object_id = OBJECT_ID(N'dbo.UserBlocks'))
    EXEC(N'CREATE UNIQUE INDEX [IX_UserBlocks_BlockerId_BlockedId] ON [UserBlocks] ([BlockerId], [BlockedId]);');

IF OBJECT_ID(N'dbo.ContentReports', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ContentReports_ContentType_ContentId_ReporterId' AND object_id = OBJECT_ID(N'dbo.ContentReports'))
    EXEC(N'CREATE UNIQUE INDEX [IX_ContentReports_ContentType_ContentId_ReporterId] ON [ContentReports] ([ContentType], [ContentId], [ReporterId]);');

IF OBJECT_ID(N'dbo.ContentReports', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ContentReports_Status_CreatedAt' AND object_id = OBJECT_ID(N'dbo.ContentReports'))
    EXEC(N'CREATE INDEX [IX_ContentReports_Status_CreatedAt] ON [ContentReports] ([Status], [CreatedAt]);');

IF OBJECT_ID(N'dbo.HiddenPosts', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HiddenPosts_UserId_PostId' AND object_id = OBJECT_ID(N'dbo.HiddenPosts'))
    EXEC(N'CREATE UNIQUE INDEX [IX_HiddenPosts_UserId_PostId] ON [HiddenPosts] ([UserId], [PostId]);');

IF OBJECT_ID(N'dbo.ChatThreads', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ChatThreads_RequestStatus_CreatedAt' AND object_id = OBJECT_ID(N'dbo.ChatThreads'))
    EXEC(N'CREATE INDEX [IX_ChatThreads_RequestStatus_CreatedAt] ON [ChatThreads] ([RequestStatus], [CreatedAt]);');

IF OBJECT_ID(N'dbo.ChatMessages', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ChatMessages_ThreadId_SeenAt' AND object_id = OBJECT_ID(N'dbo.ChatMessages'))
    EXEC(N'CREATE INDEX [IX_ChatMessages_ThreadId_SeenAt] ON [ChatMessages] ([ThreadId], [SeenAt]);');

IF OBJECT_ID(N'dbo.ChatMessages', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.ChatMessages', N'IsDeleted') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.default_constraints dc
       INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
       WHERE dc.parent_object_id = OBJECT_ID(N'dbo.ChatMessages')
         AND c.name = N'IsDeleted')
BEGIN
    EXEC(N'ALTER TABLE [ChatMessages] ADD CONSTRAINT [DF_ChatMessages_IsDeleted_Repair] DEFAULT CAST(0 AS bit) FOR [IsDeleted];');
END;
");

    }
}

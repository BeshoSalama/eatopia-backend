using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eatopia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgresSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatThreads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Pending"),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatThreads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FoodItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CaloriesPer100g = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ProteinPer100g = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FatPer100g = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CarbsPer100g = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ServingSize = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BirthDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Location = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ProfileImageUrl = table.Column<string>(type: "text", nullable: true),
                    Age = table.Column<int>(type: "integer", nullable: true),
                    WeightKg = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    HeightCm = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ActivityLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Goal = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "User"),
                    IsBanned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    BannedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BannedReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AuthProvider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Local"),
                    ExternalProviderId = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    EmailConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmailConfirmationTokenHash = table.Column<string>(type: "text", nullable: true),
                    EmailConfirmationTokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastEmailConfirmationSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedLoginAttemptCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LoginLockoutEndAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    JwtTokenVersion = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    NotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    MessageNotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CommunityNotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EmailNotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ProfileVisibility = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Public"),
                    PostsVisibility = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Public"),
                    ShowOnlineStatus = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ShowLastSeen = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AllowMessageRequests = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AllowSearchByEmail = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ThreadId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageText = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    MessageType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "text"),
                    MediaContent = table.Column<string>(type: "text", nullable: true),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    EditedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_ChatThreads_ThreadId",
                        column: x => x.ThreadId,
                        principalTable: "ChatThreads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Users_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ChatParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ThreadId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatParticipants_ChatThreads_ThreadId",
                        column: x => x.ThreadId,
                        principalTable: "ChatThreads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatParticipants_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CommunityPosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    SharedPostId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunityPosts_CommunityPosts_SharedPostId",
                        column: x => x.SharedPostId,
                        principalTable: "CommunityPosts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CommunityPosts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ContentReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportedUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Pending"),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentReports_Users_ReportedUserId",
                        column: x => x.ReportedUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ContentReports_Users_ReporterId",
                        column: x => x.ReporterId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DietPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CaloriesTargetPerDay = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    DurationDays = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DietPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DietPlans_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MealLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FoodId = table.Column<Guid>(type: "uuid", nullable: false),
                    MealImageUrl = table.Column<string>(type: "text", nullable: true),
                    DetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    QuantityGrams = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CalculatedCalories = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CalculatedProtein = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CalculatedFat = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CalculatedCarbs = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealLogs_FoodItems_FoodId",
                        column: x => x.FoodId,
                        principalTable: "FoodItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MealLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Medications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DosageText = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BeforeAfterMeal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TimesPerDay = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Medications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "info"),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ScheduledFor = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActionUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EmailSent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    EmailSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeHash = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetCodes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Recipes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CaloriesPerServing = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Servings = table.Column<int>(type: "integer", nullable: false),
                    IngredientsJson = table.Column<string>(type: "text", nullable: false),
                    StepsJson = table.Column<string>(type: "text", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recipes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recipes_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserAllergies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllergyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAllergies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAllergies_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockedId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBlocks_Users_BlockedId",
                        column: x => x.BlockedId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserBlocks_Users_BlockerId",
                        column: x => x.BlockerId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserDislikedFoods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FoodName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDislikedFoods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDislikedFoods_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserFollows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FollowerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FollowingId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFollows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserFollows_Users_FollowerId",
                        column: x => x.FollowerId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserFollows_Users_FollowingId",
                        column: x => x.FollowingId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WaterGoals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DailyTargetMl = table.Column<int>(type: "integer", nullable: false),
                    RemindEveryMinutes = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaterGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaterGoals_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WaterLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmountMl = table.Column<int>(type: "integer", nullable: false),
                    LoggedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaterLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaterLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WaterReminders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReminderDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeOfDay = table.Column<TimeSpan>(type: "interval", nullable: false),
                    AmountMl = table.Column<int>(type: "integer", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaterReminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaterReminders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comments_CommunityPosts_PostId",
                        column: x => x.PostId,
                        principalTable: "CommunityPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HiddenPosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HiddenPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HiddenPosts_CommunityPosts_PostId",
                        column: x => x.PostId,
                        principalTable: "CommunityPosts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HiddenPosts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PostLikes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostLikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostLikes_CommunityPosts_PostId",
                        column: x => x.PostId,
                        principalTable: "CommunityPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostLikes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPlans_DietPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "DietPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPlans_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MedicationSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeOfDay = table.Column<TimeSpan>(type: "interval", nullable: false),
                    IsTaken = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    TakenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicationSchedules_Medications_MedicationId",
                        column: x => x.MedicationId,
                        principalTable: "Medications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DietPlanItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    MealType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: true),
                    CaloriesEstimated = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DietPlanItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DietPlanItems_DietPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "DietPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DietPlanItems_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RecipeSaved",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeSaved", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipeSaved_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecipeSaved_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SenderId",
                table: "ChatMessages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ThreadId_SentAt",
                table: "ChatMessages",
                columns: new[] { "ThreadId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatParticipants_ThreadId_UserId",
                table: "ChatParticipants",
                columns: new[] { "ThreadId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatParticipants_UserId",
                table: "ChatParticipants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatThreads_RequestStatus_CreatedAt",
                table: "ChatThreads",
                columns: new[] { "RequestStatus", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_PostId",
                table: "Comments",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_UserId",
                table: "Comments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityPosts_IsDeleted",
                table: "CommunityPosts",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityPosts_SharedPostId",
                table: "CommunityPosts",
                column: "SharedPostId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityPosts_UserId_CreatedAt",
                table: "CommunityPosts",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentReports_ContentType_ContentId_ReporterId",
                table: "ContentReports",
                columns: new[] { "ContentType", "ContentId", "ReporterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentReports_ReportedUserId",
                table: "ContentReports",
                column: "ReportedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReports_ReporterId",
                table: "ContentReports",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReports_Status_CreatedAt",
                table: "ContentReports",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DietPlanItems_PlanId",
                table: "DietPlanItems",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_DietPlanItems_RecipeId",
                table: "DietPlanItems",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_DietPlans_CreatedBy",
                table: "DietPlans",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FoodItems_Name",
                table: "FoodItems",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_HiddenPosts_PostId",
                table: "HiddenPosts",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_HiddenPosts_UserId_PostId",
                table: "HiddenPosts",
                columns: new[] { "UserId", "PostId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MealLogs_FoodId",
                table: "MealLogs",
                column: "FoodId");

            migrationBuilder.CreateIndex(
                name: "IX_MealLogs_UserId_CreatedAt",
                table: "MealLogs",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Medications_UserId",
                table: "Medications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationSchedules_MedicationId_ScheduledDate",
                table: "MedicationSchedules",
                columns: new[] { "MedicationId", "ScheduledDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ActorUserId",
                table: "Notifications",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetCodes_UserId_ExpiresAt",
                table: "PasswordResetCodes",
                columns: new[] { "UserId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PostLikes_PostId_UserId",
                table: "PostLikes",
                columns: new[] { "PostId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostLikes_UserId",
                table: "PostLikes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_AuthorId",
                table: "Recipes",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeSaved_RecipeId",
                table: "RecipeSaved",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeSaved_UserId_RecipeId",
                table: "RecipeSaved",
                columns: new[] { "UserId", "RecipeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_ExpiresAt",
                table: "RefreshTokens",
                columns: new[] { "UserId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserAllergies_UserId_AllergyName",
                table: "UserAllergies",
                columns: new[] { "UserId", "AllergyName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserBlocks_BlockedId",
                table: "UserBlocks",
                column: "BlockedId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBlocks_BlockerId_BlockedId",
                table: "UserBlocks",
                columns: new[] { "BlockerId", "BlockedId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserDislikedFoods_UserId_FoodName",
                table: "UserDislikedFoods",
                columns: new[] { "UserId", "FoodName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserFollows_FollowerId_FollowingId",
                table: "UserFollows",
                columns: new[] { "FollowerId", "FollowingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserFollows_FollowingId",
                table: "UserFollows",
                column: "FollowingId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPlans_PlanId",
                table: "UserPlans",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPlans_UserId",
                table: "UserPlans",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true,
                filter: "[Username] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WaterGoals_UserId",
                table: "WaterGoals",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WaterLogs_UserId_LoggedAt",
                table: "WaterLogs",
                columns: new[] { "UserId", "LoggedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WaterReminders_UserId_ReminderDate_TimeOfDay",
                table: "WaterReminders",
                columns: new[] { "UserId", "ReminderDate", "TimeOfDay" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "ChatParticipants");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "ContentReports");

            migrationBuilder.DropTable(
                name: "DietPlanItems");

            migrationBuilder.DropTable(
                name: "HiddenPosts");

            migrationBuilder.DropTable(
                name: "MealLogs");

            migrationBuilder.DropTable(
                name: "MedicationSchedules");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PasswordResetCodes");

            migrationBuilder.DropTable(
                name: "PostLikes");

            migrationBuilder.DropTable(
                name: "RecipeSaved");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "UserAllergies");

            migrationBuilder.DropTable(
                name: "UserBlocks");

            migrationBuilder.DropTable(
                name: "UserDislikedFoods");

            migrationBuilder.DropTable(
                name: "UserFollows");

            migrationBuilder.DropTable(
                name: "UserPlans");

            migrationBuilder.DropTable(
                name: "WaterGoals");

            migrationBuilder.DropTable(
                name: "WaterLogs");

            migrationBuilder.DropTable(
                name: "WaterReminders");

            migrationBuilder.DropTable(
                name: "ChatThreads");

            migrationBuilder.DropTable(
                name: "FoodItems");

            migrationBuilder.DropTable(
                name: "Medications");

            migrationBuilder.DropTable(
                name: "CommunityPosts");

            migrationBuilder.DropTable(
                name: "Recipes");

            migrationBuilder.DropTable(
                name: "DietPlans");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}

using Eatopia.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Eatopia.Infrastructure.Persistence;

public class EatopiaDbContext : DbContext
{
    public EatopiaDbContext(DbContextOptions<EatopiaDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<PasswordResetCode> PasswordResetCodes => Set<PasswordResetCode>();
    public DbSet<UserAllergy> UserAllergies => Set<UserAllergy>();
    public DbSet<UserDislikedFood> UserDislikedFoods => Set<UserDislikedFood>();

    public DbSet<FoodItem> FoodItems => Set<FoodItem>();
    public DbSet<MealLog> MealLogs => Set<MealLog>();

    public DbSet<WaterGoal> WaterGoals => Set<WaterGoal>();
    public DbSet<WaterLog> WaterLogs => Set<WaterLog>();
    public DbSet<WaterReminder> WaterReminders => Set<WaterReminder>();

    public DbSet<Medication> Medications => Set<Medication>();
    public DbSet<MedicationSchedule> MedicationSchedules => Set<MedicationSchedule>();

    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeSaved> RecipeSaved => Set<RecipeSaved>();

    public DbSet<CommunityPost> CommunityPosts => Set<CommunityPost>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<PostLike> PostLikes => Set<PostLike>();
    public DbSet<UserFollow> UserFollows => Set<UserFollow>();
    public DbSet<UserBlock> UserBlocks => Set<UserBlock>();
    public DbSet<ContentReport> ContentReports => Set<ContentReport>();
    public DbSet<HiddenPost> HiddenPosts => Set<HiddenPost>();

    public DbSet<ChatThread> ChatThreads => Set<ChatThread>();
    public DbSet<ChatParticipant> ChatParticipants => Set<ChatParticipant>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    public DbSet<DietPlan> DietPlans => Set<DietPlan>();
    public DbSet<UserPlan> UserPlans => Set<UserPlan>();
    public DbSet<DietPlanItem> DietPlanItems => Set<DietPlanItem>();

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        NormalizeDateTimesForPostgres();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        NormalizeDateTimesForPostgres();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Users
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Email)
                  .IsRequired()
                  .HasMaxLength(256);

            entity.HasIndex(x => x.Email).IsUnique();

            entity.Property(x => x.Name)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(x => x.Username)
                  .HasMaxLength(100);

            entity.HasIndex(x => x.Username)
                  .IsUnique()
                  .HasFilter("[Username] IS NOT NULL");

            entity.Property(x => x.Location)
                  .HasMaxLength(100);

            entity.Property(x => x.Phone)
                  .HasMaxLength(30);

            entity.Property(x => x.ProfileImageUrl);

            entity.Property(x => x.HeightCm).HasPrecision(18, 2);
            entity.Property(x => x.WeightKg).HasPrecision(18, 2);

            entity.Property(x => x.Gender)
                  .HasMaxLength(20);

            entity.Property(x => x.ActivityLevel)
                  .HasMaxLength(50);

            entity.Property(x => x.Goal)
                  .HasMaxLength(200);

            entity.Property(x => x.Role)
                  .HasMaxLength(20)
                  .HasDefaultValue("User");

            entity.Property(x => x.IsBanned).HasDefaultValue(false);
            entity.Property(x => x.BannedAt);
            entity.Property(x => x.BannedReason).HasMaxLength(1000);

            entity.Property(x => x.AuthProvider)
                  .HasMaxLength(50)
                  .HasDefaultValue("Local");

            entity.Property(x => x.ExternalProviderId)
                  .HasMaxLength(300);

            entity.Property(x => x.EmailConfirmed)
                  .HasDefaultValue(false);

            entity.Property(x => x.EmailConfirmedAt);
            entity.Property(x => x.EmailConfirmationTokenHash);
            entity.Property(x => x.EmailConfirmationTokenExpiresAt);
            entity.Property(x => x.LastEmailConfirmationSentAt);

            entity.Property(x => x.LastSeenAt);
            entity.Property(x => x.FailedLoginAttemptCount).HasDefaultValue(0);
            entity.Property(x => x.LoginLockoutEndAt);
            entity.Property(x => x.JwtTokenVersion).HasDefaultValue(0);
            entity.Property(x => x.NotificationsEnabled).HasDefaultValue(true);
            entity.Property(x => x.MessageNotificationsEnabled).HasDefaultValue(true);
            entity.Property(x => x.CommunityNotificationsEnabled).HasDefaultValue(true);
            entity.Property(x => x.EmailNotificationsEnabled).HasDefaultValue(true);
            entity.Property(x => x.ProfileVisibility).HasMaxLength(20).HasDefaultValue("Public");
            entity.Property(x => x.PostsVisibility).HasMaxLength(20).HasDefaultValue("Public");
            entity.Property(x => x.ShowOnlineStatus).HasDefaultValue(true);
            entity.Property(x => x.ShowLastSeen).HasDefaultValue(true);
            entity.Property(x => x.AllowMessageRequests).HasDefaultValue(true);
            entity.Property(x => x.AllowSearchByEmail).HasDefaultValue(true);
        });


        modelBuilder.Entity<PasswordResetCode>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.User)
                  .WithMany()
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.Property(x => x.CodeHash).IsRequired();
            entity.Property(x => x.ExpiresAt).IsRequired();
            entity.Property(x => x.IsUsed).HasDefaultValue(false);
            entity.Property(x => x.AttemptCount).HasDefaultValue(0);
            entity.Property(x => x.LastAttemptAt);
            entity.HasIndex(x => new { x.UserId, x.ExpiresAt });
        });

        modelBuilder.Entity<UserAllergy>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.AllergyName)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.HasOne(x => x.User)
                  .WithMany(u => u.Allergies)
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(x => new { x.UserId, x.AllergyName }).IsUnique();
        });

        modelBuilder.Entity<UserDislikedFood>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.FoodName)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.HasOne(x => x.User)
                  .WithMany(u => u.DislikedFoods)
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(x => new { x.UserId, x.FoodName }).IsUnique();
        });

        // Food + Meals
        modelBuilder.Entity<FoodItem>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(x => x.CaloriesPer100g).HasPrecision(18, 2);
            entity.Property(x => x.ProteinPer100g).HasPrecision(18, 2);
            entity.Property(x => x.FatPer100g).HasPrecision(18, 2);
            entity.Property(x => x.CarbsPer100g).HasPrecision(18, 2);

            entity.HasIndex(x => x.Name);
        });

        modelBuilder.Entity<MealLog>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.User)
                  .WithMany()
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.FoodItem)
                  .WithMany()
                  .HasForeignKey(x => x.FoodId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.Property(x => x.QuantityGrams).HasPrecision(18, 2);
            entity.Property(x => x.CalculatedCalories).HasPrecision(18, 2);
            entity.Property(x => x.CalculatedProtein).HasPrecision(18, 2);
            entity.Property(x => x.CalculatedFat).HasPrecision(18, 2);
            entity.Property(x => x.CalculatedCarbs).HasPrecision(18, 2);

            entity.HasIndex(x => new { x.UserId, x.CreatedAt });
        });

        // Water
        modelBuilder.Entity<WaterGoal>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.User)
                  .WithMany()
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.Property(x => x.DailyTargetMl).IsRequired();
            entity.Property(x => x.RemindEveryMinutes).IsRequired();

            entity.HasIndex(x => x.UserId).IsUnique();
        });

        modelBuilder.Entity<WaterLog>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.User)
                  .WithMany()
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.Property(x => x.AmountMl).IsRequired();
            entity.Property(x => x.LoggedAt).IsRequired();

            entity.HasIndex(x => new { x.UserId, x.LoggedAt });
        });

        modelBuilder.Entity<WaterReminder>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.User)
                  .WithMany()
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.Property(x => x.ReminderDate).IsRequired();
            entity.Property(x => x.TimeOfDay).IsRequired();
            entity.Property(x => x.AmountMl).IsRequired();
            entity.Property(x => x.IsCompleted).HasDefaultValue(false);

            entity.HasIndex(x => new { x.UserId, x.ReminderDate, x.TimeOfDay });
        });

        // Medication
        modelBuilder.Entity<Medication>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.User)
                  .WithMany()
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.Property(x => x.Name)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(x => x.DosageText)
                  .HasMaxLength(200);

            entity.Property(x => x.BeforeAfterMeal)
                  .HasMaxLength(20);

            entity.Property(x => x.TimesPerDay)
                  .IsRequired();
        });

        modelBuilder.Entity<MedicationSchedule>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.Medication)
                  .WithMany()
                  .HasForeignKey(x => x.MedicationId);

            entity.Property(x => x.ScheduledDate).IsRequired();
            entity.Property(x => x.TimeOfDay).IsRequired();

            entity.Property(x => x.IsTaken).HasDefaultValue(false);

            entity.HasIndex(x => new { x.MedicationId, x.ScheduledDate });
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.User)
                  .WithMany()
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.ActorUser)
                  .WithMany()
                  .HasForeignKey(x => x.ActorUserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.Property(x => x.Title).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Message).IsRequired();
            entity.Property(x => x.Type).HasMaxLength(50).HasDefaultValue("info");
            entity.Property(x => x.RelatedEntityType).HasMaxLength(100);
            entity.Property(x => x.ActionUrl).HasMaxLength(500);
            entity.Property(x => x.IsRead).HasDefaultValue(false);
            entity.Property(x => x.EmailSent).HasDefaultValue(false);

            entity.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt });
        });

        // Recipes
        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Title)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.ImageUrl).HasMaxLength(2000);
            entity.Property(x => x.IngredientsJson).IsRequired();
            entity.Property(x => x.StepsJson).IsRequired();
            entity.Property(x => x.CaloriesPerServing).HasPrecision(18, 2);

            entity.HasOne(x => x.Author)
                  .WithMany()
                  .HasForeignKey(x => x.AuthorId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<RecipeSaved>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.User)
                  .WithMany()
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.Recipe)
                  .WithMany()
                  .HasForeignKey(x => x.RecipeId);

            entity.HasIndex(x => new { x.UserId, x.RecipeId }).IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TokenHash).IsRequired();
            entity.Property(x => x.ExpiresAt).IsRequired();
            entity.HasOne(x => x.User)
                  .WithMany()
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.NoAction);
            entity.HasIndex(x => new { x.UserId, x.ExpiresAt });
        });

        // Community
        modelBuilder.Entity<CommunityPost>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Content).IsRequired();
            entity.Property(x => x.ImageUrl);
            entity.Property(x => x.IsDeleted).HasDefaultValue(false);
            entity.Property(x => x.DeletedAt);
            entity.HasIndex(x => new { x.UserId, x.CreatedAt });
            entity.HasIndex(x => x.IsDeleted);

            entity.HasOne(x => x.User)
                  .WithMany()
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.NoAction); // 👈 مهم

            entity.HasOne(x => x.SharedPost)
                  .WithMany()
                  .HasForeignKey(x => x.SharedPostId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Text).IsRequired();

            entity.HasOne(x => x.Post)
                  .WithMany()
                  .HasForeignKey(x => x.PostId)
                  .OnDelete(DeleteBehavior.Cascade); // 👈 سيب دي Cascade

            entity.HasOne(x => x.User)
                  .WithMany()
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.NoAction); // 👈 غير دي
        });

        modelBuilder.Entity<PostLike>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.Post)
                  .WithMany()
                  .HasForeignKey(x => x.PostId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                  .WithMany()
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.NoAction); // 👈 مهم

            entity.HasIndex(x => new { x.PostId, x.UserId }).IsUnique();
        });

        modelBuilder.Entity<UserBlock>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.Blocker)
                  .WithMany()
                  .HasForeignKey(x => x.BlockerId)
                  .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.Blocked)
                  .WithMany()
                  .HasForeignKey(x => x.BlockedId)
                  .OnDelete(DeleteBehavior.NoAction);
            entity.HasIndex(x => new { x.BlockerId, x.BlockedId }).IsUnique();
        });

        modelBuilder.Entity<ContentReport>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ContentType).IsRequired().HasMaxLength(30);
            entity.Property(x => x.Reason).IsRequired().HasMaxLength(1000);
            entity.Property(x => x.Status).HasMaxLength(30).HasDefaultValue("Pending");
            entity.HasOne(x => x.Reporter)
                  .WithMany()
                  .HasForeignKey(x => x.ReporterId)
                  .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.ReportedUser)
                  .WithMany()
                  .HasForeignKey(x => x.ReportedUserId)
                  .OnDelete(DeleteBehavior.NoAction);
            entity.HasIndex(x => new { x.ContentType, x.ContentId, x.ReporterId }).IsUnique();
            entity.HasIndex(x => new { x.Status, x.CreatedAt });
        });

        modelBuilder.Entity<HiddenPost>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.User)
                  .WithMany()
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.Post)
                  .WithMany()
                  .HasForeignKey(x => x.PostId)
                  .OnDelete(DeleteBehavior.NoAction);
            entity.HasIndex(x => new { x.UserId, x.PostId }).IsUnique();
        });

        modelBuilder.Entity<UserFollow>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.Follower)
                  .WithMany()
                  .HasForeignKey(x => x.FollowerId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.Following)
                  .WithMany()
                  .HasForeignKey(x => x.FollowingId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(x => new { x.FollowerId, x.FollowingId }).IsUnique();
        });

        // Chat
        modelBuilder.Entity<ChatThread>(entity =>
        {
            entity.Property(x => x.RequestStatus).HasMaxLength(30).HasDefaultValue("Pending");
            entity.Property(x => x.AcceptedAt);
            entity.Property(x => x.DeletedAt);
            entity.HasIndex(x => new { x.RequestStatus, x.CreatedAt });
        });

        modelBuilder.Entity<ChatParticipant>()
            .HasIndex(x => new { x.ThreadId, x.UserId })
            .IsUnique();

        modelBuilder.Entity<ChatMessage>()
            .HasIndex(x => new { x.ThreadId, x.SentAt });

        modelBuilder.Entity<ChatParticipant>(entity =>
        {
            entity.HasOne(x => x.Thread)
                  .WithMany(t => t.Participants)
                  .HasForeignKey(x => x.ThreadId);

            entity.HasOne(x => x.User)
                  .WithMany()
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.Property(x => x.MessageText).HasDefaultValue(string.Empty);
            entity.Property(x => x.MessageType).HasMaxLength(30).HasDefaultValue("text");
            entity.Property(x => x.MediaContent);
            entity.Property(x => x.FileName).HasMaxLength(255);
            // Do not configure IsDeleted as store-generated here. Some existing local databases
            // have the NOT NULL column without a default constraint; if EF omits
            // the value, the database tries to insert NULL and chat send fails.
            entity.Property(x => x.IsDeleted).IsRequired();
            entity.Property(x => x.EditedAt);
            entity.Property(x => x.DeletedAt);
            entity.Property(x => x.DeliveredAt);
            entity.Property(x => x.SeenAt);

            entity.HasOne(x => x.Thread)
                  .WithMany(t => t.Messages)
                  .HasForeignKey(x => x.ThreadId);

            entity.HasOne(x => x.Sender)
                  .WithMany()
                  .HasForeignKey(x => x.SenderId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // Diet Plans
        modelBuilder.Entity<DietPlan>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Title)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(x => x.CaloriesTargetPerDay).HasPrecision(18, 2);

            entity.HasOne(x => x.Creator)
                  .WithMany()
                  .HasForeignKey(x => x.CreatedBy)
                  .OnDelete(DeleteBehavior.NoAction); // 👈 مهم
        });

        modelBuilder.Entity<UserPlan>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.User)
                  .WithMany()
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.NoAction); // 👈 مهم

            entity.HasOne(x => x.Plan)
                  .WithMany()
                  .HasForeignKey(x => x.PlanId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DietPlanItem>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.MealType)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(x => x.Title)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(x => x.CaloriesEstimated).HasPrecision(18, 2);

            entity.HasOne(x => x.Plan)
                  .WithMany(p => p.Items)
                  .HasForeignKey(x => x.PlanId);

            entity.HasOne(x => x.Recipe)
                  .WithMany()
                  .HasForeignKey(x => x.RecipeId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        base.OnModelCreating(modelBuilder);
    }

    private void NormalizeDateTimesForPostgres()
    {
        foreach (var entry in ChangeTracker.Entries().Where(x => x.State is EntityState.Added or EntityState.Modified))
        {
            foreach (var property in entry.Properties)
            {
                var propertyType = Nullable.GetUnderlyingType(property.Metadata.ClrType) ?? property.Metadata.ClrType;
                if (propertyType != typeof(DateTime) || property.CurrentValue is not DateTime dateTime)
                    continue;

                property.CurrentValue = EnsureUtc(dateTime);
            }
        }
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}

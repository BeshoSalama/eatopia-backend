using Eatopia.Domain.Entities;
using Eatopia.Domain.Auth;

namespace Eatopia.Infrastructure.Persistence;

public static class DbSeeder
{
    private const string OwnerEmail = "fadynour194@gmail.com";
    private const string LegacyOwnerEmail = "admin@eatopia.com";
    private const string OwnerPassword = "Admin12345";

    public static void Seed(EatopiaDbContext context)
    {
        // Users
        if (!context.Users.Any())
        {
            var owner = new User
            {
                Id = Guid.NewGuid(),
                Email = OwnerEmail,
                Username = "fadynour194",
                Name = "Fady Nour",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(OwnerPassword),
                Role = UserRoles.Owner,
                Location = "Cairo",
                Gender = "male",
                EmailConfirmed = true,
                EmailConfirmedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            // Keep development seeding clean: seed only the owner account.
            // Test/community users should be created through the normal signup flow
            // so they do not appear as fake message-request/demo users.
            context.Users.Add(owner);
            context.SaveChanges();
        }

        var ownerAccount = context.Users.FirstOrDefault(x => x.Email == OwnerEmail);

        if (ownerAccount is null)
        {
            ownerAccount = new User
            {
                Id = Guid.NewGuid(),
                Email = OwnerEmail,
                Username = "fadynour194",
                Name = "Fady Nour",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(OwnerPassword),
                Role = UserRoles.Owner,
                Location = "Cairo",
                Gender = "male",
                EmailConfirmed = true,
                EmailConfirmedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(ownerAccount);
            context.SaveChanges();
        }
        else if (ownerAccount is not null && NeedsOwnerSeedRepair(ownerAccount))
        {
            PromoteOwnerAccount(ownerAccount);
            context.SaveChanges();
        }

        var legacyOwnerAccount = context.Users.FirstOrDefault(x => x.Email == LegacyOwnerEmail);
        if (legacyOwnerAccount is not null && legacyOwnerAccount.Role == UserRoles.Owner)
        {
            legacyOwnerAccount.Role = UserRoles.Admin;
            legacyOwnerAccount.JwtTokenVersion += 1;
            context.SaveChanges();
        }

        // Food items
        if (!context.FoodItems.Any())
        {
            var foods = new List<FoodItem>
            {
                new() { Id = Guid.NewGuid(), Name = "Pizza", CaloriesPer100g = 266, ProteinPer100g = 11, FatPer100g = 10, CarbsPer100g = 33, ServingSize = "100g" },
                new() { Id = Guid.NewGuid(), Name = "Apple", CaloriesPer100g = 52, ProteinPer100g = 0.3m, FatPer100g = 0.2m, CarbsPer100g = 14, ServingSize = "100g" },
                new() { Id = Guid.NewGuid(), Name = "Chicken Breast", CaloriesPer100g = 165, ProteinPer100g = 31, FatPer100g = 3.6m, CarbsPer100g = 0, ServingSize = "100g" },
                new() { Id = Guid.NewGuid(), Name = "Rice Cooked", CaloriesPer100g = 130, ProteinPer100g = 2.7m, FatPer100g = 0.3m, CarbsPer100g = 28, ServingSize = "100g" },
                new() { Id = Guid.NewGuid(), Name = "Banana", CaloriesPer100g = 89, ProteinPer100g = 1.1m, FatPer100g = 0.3m, CarbsPer100g = 23, ServingSize = "100g" },
                new() { Id = Guid.NewGuid(), Name = "Egg", CaloriesPer100g = 155, ProteinPer100g = 13, FatPer100g = 11, CarbsPer100g = 1.1m, ServingSize = "100g" },
                new() { Id = Guid.NewGuid(), Name = "Oats", CaloriesPer100g = 389, ProteinPer100g = 17, FatPer100g = 7, CarbsPer100g = 66, ServingSize = "100g" },
                new() { Id = Guid.NewGuid(), Name = "Milk", CaloriesPer100g = 42, ProteinPer100g = 3.4m, FatPer100g = 1, CarbsPer100g = 5, ServingSize = "100g" },
                new() { Id = Guid.NewGuid(), Name = "Salad", CaloriesPer100g = 20, ProteinPer100g = 1, FatPer100g = 0.2m, CarbsPer100g = 3.6m, ServingSize = "100g" },
                new() { Id = Guid.NewGuid(), Name = "Orange", CaloriesPer100g = 47, ProteinPer100g = 0.9m, FatPer100g = 0.1m, CarbsPer100g = 12, ServingSize = "100g" }
            };

            context.FoodItems.AddRange(foods);
            context.SaveChanges();
        }

        // Recipes
        if (!context.Recipes.Any())
        {
            var adminId = context.Users.FirstOrDefault(x => x.Role == UserRoles.Owner || x.Role == UserRoles.Admin)?.Id;

            context.Recipes.AddRange(
                new Recipe
                {
                    Id = Guid.NewGuid(),
                    Title = "Healthy Oatmeal",
                    CaloriesPerServing = 300,
                    Servings = 1,
                    IngredientsJson = "[{\"name\":\"Oats\",\"quantity\":\"50g\"},{\"name\":\"Milk\",\"quantity\":\"200ml\"}]",
                    StepsJson = "[\"Add oats\",\"Add milk\",\"Cook for 5 minutes\"]",
                    AuthorId = adminId
                },
                new Recipe
                {
                    Id = Guid.NewGuid(),
                    Title = "Chicken Salad",
                    CaloriesPerServing = 450,
                    Servings = 1,
                    IngredientsJson = "[{\"name\":\"Chicken Breast\",\"quantity\":\"150g\"},{\"name\":\"Salad\",\"quantity\":\"200g\"}]",
                    StepsJson = "[\"Grill chicken\",\"Mix with salad\",\"Serve\"]",
                    AuthorId = adminId
                }
            );

            context.SaveChanges();
        }
    }

    public static void SeedOwner(
        EatopiaDbContext context,
        string email,
        string password,
        string username = "owner",
        string name = "Eatopia Owner")
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("Owner seed email is required.");

        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            throw new InvalidOperationException("Owner seed password must be at least 8 characters.");

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var ownerAccount = context.Users.FirstOrDefault(x => x.Email == normalizedEmail);

        if (ownerAccount is null)
        {
            ownerAccount = new User
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                Username = string.IsNullOrWhiteSpace(username) ? normalizedEmail.Split('@')[0] : username.Trim(),
                Name = string.IsNullOrWhiteSpace(name) ? "Eatopia Owner" : name.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = UserRoles.Owner,
                EmailConfirmed = true,
                EmailConfirmedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(ownerAccount);
            context.SaveChanges();
            return;
        }

        PromoteOwnerAccount(ownerAccount, username, name);
        context.SaveChanges();
    }

    private static void PromoteOwnerAccount(User ownerAccount, string username = "fadynour194", string name = "Fady Nour")
    {
        ownerAccount.Role = UserRoles.Owner;
        ownerAccount.EmailConfirmed = true;
        ownerAccount.EmailConfirmedAt ??= DateTime.UtcNow;
        ownerAccount.Username = string.IsNullOrWhiteSpace(ownerAccount.Username) ? username : ownerAccount.Username;
        ownerAccount.Name = string.IsNullOrWhiteSpace(ownerAccount.Name) ? name : ownerAccount.Name;
        ownerAccount.JwtTokenVersion += 1;
    }

    private static bool NeedsOwnerSeedRepair(User ownerAccount) =>
        ownerAccount.Role != UserRoles.Owner ||
        !ownerAccount.EmailConfirmed ||
        string.IsNullOrWhiteSpace(ownerAccount.Username) ||
        string.IsNullOrWhiteSpace(ownerAccount.Name);
}

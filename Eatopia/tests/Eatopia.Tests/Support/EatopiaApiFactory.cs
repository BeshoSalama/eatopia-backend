using Eatopia.Domain.Auth;
using Eatopia.Domain.Entities;
using Eatopia.Infrastructure.Persistence;
using Eatopia.Infrastructure.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Eatopia.Tests.Support;

public sealed class EatopiaApiFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;
    private readonly string _uploadsRoot = Path.Combine(Path.GetTempPath(), "eatopia-tests", Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:",
                ["Jwt:Key"] = "EatopiaIntegrationTestsJwtKeyMustBeLongEnough123!",
                ["Jwt:Issuer"] = "Eatopia.Tests",
                ["Jwt:Audience"] = "Eatopia.Tests",
                ["Jwt:DurationInMinutes"] = "60",
                ["Frontend:BaseUrl"] = "http://localhost:3000",
                ["Email:BypassWhenMissingInDevelopment"] = "true",
                ["Email:Password"] = "",
                ["MediaStorage:UploadsRoot"] = _uploadsRoot
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<EatopiaDbContext>>();

            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<EatopiaDbContext>(options =>
                options.UseSqlite(_connection).EnableSensitiveDataLogging());
        });
    }

    public async Task ResetDatabaseAsync()
    {
        await WithDbAsync(async db =>
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
        });
    }

    public async Task<User> AddUserAsync(
        string email,
        string role = UserRoles.User,
        string password = "ValidPass1!",
        bool emailConfirmed = true)
    {
        return await WithDbAsync(async db =>
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email.Trim().ToLowerInvariant(),
                Username = email.Split('@')[0],
                Name = email.Split('@')[0],
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = role,
                AuthProvider = "Local",
                EmailConfirmed = emailConfirmed,
                EmailConfirmedAt = emailConfirmed ? DateTime.UtcNow : null,
                CreatedAt = DateTime.UtcNow
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user;
        });
    }

    public string CreateToken(User user)
    {
        using var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<JwtService>().GenerateToken(user);
    }

    public async Task WithDbAsync(Func<EatopiaDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EatopiaDbContext>();
        await action(db);
    }

    public async Task<T> WithDbAsync<T>(Func<EatopiaDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EatopiaDbContext>();
        return await action(db);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection?.Dispose();
        if (disposing && Directory.Exists(_uploadsRoot))
        {
            try { Directory.Delete(_uploadsRoot, recursive: true); }
            catch { }
        }
    }
}

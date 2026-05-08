using Eatopia.Domain.Auth;
using Eatopia.Domain.Entities;
using Eatopia.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Eatopia.Tests.Support;

public sealed class TestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private TestDatabase(SqliteConnection connection, EatopiaDbContext context)
    {
        _connection = connection;
        Context = context;
    }

    public EatopiaDbContext Context { get; }

    public static async Task<TestDatabase> CreateAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<EatopiaDbContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .Options;

        var context = new EatopiaDbContext(options);
        await context.Database.EnsureCreatedAsync();

        return new TestDatabase(connection, context);
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    public async Task<User> AddUserAsync(
        string email,
        string role = UserRoles.User,
        string password = "ValidPass1!",
        bool emailConfirmed = true)
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

        Context.Users.Add(user);
        await Context.SaveChangesAsync();
        return user;
    }
}

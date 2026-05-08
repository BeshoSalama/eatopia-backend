using Eatopia.Application.DTOs.Auth;
using Eatopia.Application.Exceptions;
using Eatopia.Domain.Entities;
using Eatopia.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace Eatopia.Tests.Services;

public class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_NormalizesEmailAndBypassesActivationWhenDevelopmentEmailIsMissing()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = TestServices.AuthService(database);

        var response = await service.RegisterAsync(new RegisterRequestDto
        {
            FullName = "Fady Nour",
            Email = "  FADY@EXAMPLE.COM ",
            Password = "ValidPass1!",
            Username = "FadyNour",
            BirthDate = new DateTime(2000, 1, 1),
            Gender = "male",
            Location = "Cairo"
        });

        var user = await database.Context.Users.SingleAsync(x => x.Id == response.User.Id);
        Assert.Equal("fady@example.com", user.Email);
        Assert.Equal("User", user.Role);
        Assert.True(user.EmailConfirmed);
        Assert.Null(user.EmailConfirmationTokenHash);
    }

    [Fact]
    public async Task LoginAsync_UnconfirmedLocalUserWithPendingToken_ThrowsEmailNotConfirmed()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await database.AddUserAsync("pending@eatopia.local", emailConfirmed: false);
        user.EmailConfirmationTokenHash = BCrypt.Net.BCrypt.HashPassword("pending-token");
        user.EmailConfirmationTokenExpiresAt = DateTime.UtcNow.AddHours(1);
        await database.Context.SaveChangesAsync();
        var service = TestServices.AuthService(database);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.LoginAsync(new LoginRequestDto
            {
                UsernameOrEmail = user.Email,
                Password = "ValidPass1!"
            }));

        Assert.Equal(403, exception.StatusCode);
        Assert.Equal("EMAIL_NOT_CONFIRMED", exception.Code);
    }

    [Fact]
    public async Task LoginAsync_FiveBadPasswords_LocksAccountAndRejectsCorrectPasswordDuringLockout()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await database.AddUserAsync("locked@eatopia.local");
        var service = TestServices.AuthService(database);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await Assert.ThrowsAsync<ApiException>(() =>
                service.LoginAsync(new LoginRequestDto
                {
                    UsernameOrEmail = user.Email,
                    Password = "WrongPass1!"
                }));
        }

        var reloaded = await database.Context.Users.SingleAsync(x => x.Id == user.Id);
        Assert.NotNull(reloaded.LoginLockoutEndAt);
        Assert.True(reloaded.LoginLockoutEndAt > DateTime.UtcNow);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.LoginAsync(new LoginRequestDto
            {
                UsernameOrEmail = user.Email,
                Password = "ValidPass1!"
            }));

        Assert.Equal(429, exception.StatusCode);
        Assert.Equal("LOGIN_LOCKED", exception.Code);
    }
}

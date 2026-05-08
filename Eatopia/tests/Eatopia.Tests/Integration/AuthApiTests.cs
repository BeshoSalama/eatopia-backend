using Eatopia.Tests.Support;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Eatopia.Tests.Integration;

public class AuthApiTests
{
    [Fact]
    public async Task Register_then_login_returns_a_valid_token()
    {
        using var factory = new EatopiaApiFactory();
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "New.User@Test.com",
            password = "StrongPass1!",
            fullName = "New User",
            username = "newuser"
        });

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        await factory.WithDbAsync(async db =>
        {
            var user = await db.Users.SingleAsync(x => x.Email == "new.user@test.com");
            Assert.Equal("New User", user.Name);
            Assert.True(user.EmailConfirmed);
        });

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            usernameOrEmail = "new.user@test.com",
            password = "StrongPass1!"
        });

        loginResponse.EnsureSuccessStatusCode();
        using var json = await JsonDocument.ParseAsync(await loginResponse.Content.ReadAsStreamAsync());
        var token = json.RootElement.GetProperty("data").GetProperty("token").GetString();

        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public async Task Register_rejects_duplicate_email()
    {
        using var factory = new EatopiaApiFactory();
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        var payload = new
        {
            email = "duplicate@test.com",
            password = "StrongPass1!",
            fullName = "Duplicate User",
            username = "duplicateuser"
        };

        var first = await client.PostAsJsonAsync("/api/v1/auth/register", payload);
        var second = await client.PostAsJsonAsync("/api/v1/auth/register", payload);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }
}

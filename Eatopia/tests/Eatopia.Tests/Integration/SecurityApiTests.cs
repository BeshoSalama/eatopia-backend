using Eatopia.Domain.Auth;
using Eatopia.Tests.Support;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Eatopia.Tests.Integration;

public class SecurityApiTests
{
    [Theory]
    [InlineData("/api/profile")]
    [InlineData("/api/chat/threads")]
    [InlineData("/api/v1/diet-plans/generate")]
    [InlineData("/api/uploads")]
    public async Task Protected_endpoints_reject_anonymous_requests(string url)
    {
        using var factory = new EatopiaApiFactory();
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        var response = url.EndsWith("/generate", StringComparison.OrdinalIgnoreCase)
            ? await client.PostAsJsonAsync(url, new { durationDays = 1, caloriesTargetPerDay = 1800 })
            : url.EndsWith("/uploads", StringComparison.OrdinalIgnoreCase)
                ? await client.PostAsync(url, new MultipartFormDataContent())
                : await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Banning_user_revokes_existing_access_token()
    {
        using var factory = new EatopiaApiFactory();
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        var owner = await factory.AddUserAsync("owner@test.com", UserRoles.Owner);
        var member = await factory.AddUserAsync("member@test.com", UserRoles.User);
        var memberToken = factory.CreateToken(member);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        var beforeBan = await client.GetAsync("/api/profile");
        beforeBan.EnsureSuccessStatusCode();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken(owner));
        var banResponse = await client.PutAsJsonAsync($"/api/admin/users/{member.Id}/ban", new { reason = "Pre-deploy token revocation check" });
        banResponse.EnsureSuccessStatusCode();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        var afterBan = await client.GetAsync("/api/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, afterBan.StatusCode);
        await factory.WithDbAsync(async db =>
        {
            var stored = await db.Users.SingleAsync(x => x.Id == member.Id);
            Assert.True(stored.IsBanned);
            Assert.Equal(1, stored.JwtTokenVersion);
        });
    }
}

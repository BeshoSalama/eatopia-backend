using Eatopia.Domain.Auth;
using Eatopia.Domain.Entities;
using Eatopia.Tests.Support;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Eatopia.Tests.Integration;

public class CommunityReportsApiTests
{
    [Fact]
    public async Task User_can_report_post_and_admin_can_see_it()
    {
        using var factory = new EatopiaApiFactory();
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        var reporter = await factory.AddUserAsync("reporter@test.com");
        var author = await factory.AddUserAsync("author@test.com");
        var admin = await factory.AddUserAsync("admin@test.com", UserRoles.Admin);
        var postId = await factory.WithDbAsync(async db =>
        {
            var post = new CommunityPost
            {
                Id = Guid.NewGuid(),
                UserId = author.Id,
                Content = "This post will be reported in integration tests.",
                CreatedAt = DateTime.UtcNow
            };

            db.CommunityPosts.Add(post);
            await db.SaveChangesAsync();
            return post.Id;
        });

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken(reporter));
        var reportResponse = await client.PostAsJsonAsync($"/api/community/posts/{postId}/report", new
        {
            reason = "Offensive wording"
        });

        reportResponse.EnsureSuccessStatusCode();

        var duplicateResponse = await client.PostAsJsonAsync($"/api/community/posts/{postId}/report", new
        {
            reason = "Same report again"
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken(admin));
        var adminReports = await client.GetAsync("/api/admin/reports?status=Pending");

        adminReports.EnsureSuccessStatusCode();
        using var json = await JsonDocument.ParseAsync(await adminReports.Content.ReadAsStreamAsync());
        var reports = json.RootElement.GetProperty("reports");

        Assert.Single(reports.EnumerateArray());
        Assert.Equal("Post", reports[0].GetProperty("content_type").GetString());
        Assert.Equal("Offensive wording", reports[0].GetProperty("reason").GetString());

        await factory.WithDbAsync(async db =>
        {
            var storedReport = await db.ContentReports.SingleAsync();
            Assert.Equal(reporter.Id, storedReport.ReporterId);
            Assert.Equal(author.Id, storedReport.ReportedUserId);
        });
    }
}

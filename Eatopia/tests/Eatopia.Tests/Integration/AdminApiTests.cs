using Eatopia.Domain.Auth;
using Eatopia.Domain.Entities;
using Eatopia.Tests.Support;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Eatopia.Tests.Integration;

public class AdminApiTests
{
    [Fact]
    public async Task Admin_endpoints_enforce_role_rules()
    {
        using var factory = new EatopiaApiFactory();
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        var unauthenticatedStats = await client.GetAsync("/api/admin/stats");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticatedStats.StatusCode);

        var owner = await factory.AddUserAsync("owner@test.com", UserRoles.Owner);
        var admin = await factory.AddUserAsync("manager@test.com", UserRoles.Admin);
        var regularUser = await factory.AddUserAsync("member@test.com", UserRoles.User);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken(admin));
        var adminRoleChange = await client.PutAsJsonAsync($"/api/admin/users/{regularUser.Id}/role", new { role = UserRoles.Owner });
        Assert.Equal(HttpStatusCode.Forbidden, adminRoleChange.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken(owner));
        var ownerBan = await client.PutAsJsonAsync($"/api/admin/users/{regularUser.Id}/ban", new { reason = "Integration moderation check" });
        ownerBan.EnsureSuccessStatusCode();

        await factory.WithDbAsync(async db =>
        {
            var updated = await db.Users.SingleAsync(x => x.Id == regularUser.Id);
            Assert.True(updated.IsBanned);
            Assert.Equal("Integration moderation check", updated.BannedReason);
            Assert.Equal(1, updated.JwtTokenVersion);
        });
    }

    [Fact]
    public async Task Admin_cannot_ban_owner_or_another_admin()
    {
        using var factory = new EatopiaApiFactory();
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        var admin = await factory.AddUserAsync("manager@test.com", UserRoles.Admin);
        var secondAdmin = await factory.AddUserAsync("second-manager@test.com", UserRoles.Admin);
        var owner = await factory.AddUserAsync("owner@test.com", UserRoles.Owner);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken(admin));

        var banAdmin = await client.PutAsJsonAsync($"/api/admin/users/{secondAdmin.Id}/ban", new { reason = "Should not work" });
        var banOwner = await client.PutAsJsonAsync($"/api/admin/users/{owner.Id}/ban", new { reason = "Should not work" });

        Assert.Equal(HttpStatusCode.Forbidden, banAdmin.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, banOwner.StatusCode);

        await factory.WithDbAsync(async db =>
        {
            Assert.False(await db.Users.Where(x => x.Id == secondAdmin.Id || x.Id == owner.Id).AnyAsync(x => x.IsBanned));
        });
    }

    [Fact]
    public async Task Report_actions_warn_user_and_remove_reported_post()
    {
        using var factory = new EatopiaApiFactory();
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        var admin = await factory.AddUserAsync("manager@test.com", UserRoles.Admin);
        var reporter = await factory.AddUserAsync("reporter@test.com", UserRoles.User);
        var secondReporter = await factory.AddUserAsync("second-reporter@test.com", UserRoles.User);
        var author = await factory.AddUserAsync("author@test.com", UserRoles.User);

        var ids = await factory.WithDbAsync(async db =>
        {
            var post = new CommunityPost
            {
                Id = Guid.NewGuid(),
                UserId = author.Id,
                Content = "Reported post content.",
                CreatedAt = DateTime.UtcNow
            };
            var warningReport = new ContentReport
            {
                Id = Guid.NewGuid(),
                ReporterId = reporter.Id,
                ReportedUserId = author.Id,
                ContentType = "Post",
                ContentId = post.Id,
                Reason = "Unsafe advice",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };
            var removalReport = new ContentReport
            {
                Id = Guid.NewGuid(),
                ReporterId = secondReporter.Id,
                ReportedUserId = author.Id,
                ContentType = "Post",
                ContentId = post.Id,
                Reason = "Needs removal",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            db.CommunityPosts.Add(post);
            db.ContentReports.AddRange(warningReport, removalReport);
            await db.SaveChangesAsync();
            return new { PostId = post.Id, WarningReportId = warningReport.Id, RemovalReportId = removalReport.Id };
        });

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken(admin));

        var warnResponse = await client.PostAsJsonAsync($"/api/admin/reports/{ids.WarningReportId}/action", new
        {
            action = "warn-user",
            note = "Please keep advice safe and respectful."
        });
        var removeResponse = await client.PostAsJsonAsync($"/api/admin/reports/{ids.RemovalReportId}/action", new
        {
            action = "delete-content",
            note = ""
        });

        warnResponse.EnsureSuccessStatusCode();
        removeResponse.EnsureSuccessStatusCode();

        await factory.WithDbAsync(async db =>
        {
            var warningReport = await db.ContentReports.SingleAsync(x => x.Id == ids.WarningReportId);
            var removalReport = await db.ContentReports.SingleAsync(x => x.Id == ids.RemovalReportId);
            var post = await db.CommunityPosts.SingleAsync(x => x.Id == ids.PostId);
            var notification = await db.Notifications.SingleAsync(x => x.UserId == author.Id && x.Type == "moderation_warning");

            Assert.Equal("Actioned", warningReport.Status);
            Assert.Equal("Actioned", removalReport.Status);
            Assert.True(post.IsDeleted);
            Assert.Equal("Please keep advice safe and respectful.", notification.Message);
            Assert.Equal(admin.Id, notification.ActorUserId);
        });
    }
}

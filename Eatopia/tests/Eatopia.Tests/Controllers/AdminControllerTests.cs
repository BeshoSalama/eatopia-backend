using Eatopia.Api.Controllers;
using Eatopia.Application.Exceptions;
using Eatopia.Domain.Auth;
using Eatopia.Domain.Entities;
using Eatopia.Tests.Support;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Eatopia.Tests.Controllers;

public class AdminControllerTests
{
    [Fact]
    public async Task UpdateUserRole_WhenCallerIsAdmin_ThrowsForbidden()
    {
        await using var database = await TestDatabase.CreateAsync();
        var admin = await database.AddUserAsync("admin@eatopia.local", UserRoles.Admin);
        var target = await database.AddUserAsync("target@eatopia.local");
        var controller = ControllerAs(database, admin.Id, UserRoles.Admin);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            controller.UpdateUserRole(target.Id, new UpdateUserRoleDto { Role = UserRoles.Admin }));

        Assert.Equal(403, exception.StatusCode);
        Assert.Equal("FORBIDDEN", exception.Code);
    }

    [Fact]
    public async Task BanUser_WhenTargetIsOwner_ThrowsForbidden()
    {
        await using var database = await TestDatabase.CreateAsync();
        var owner = await database.AddUserAsync("owner@eatopia.local", UserRoles.Owner);
        var otherOwner = await database.AddUserAsync("other-owner@eatopia.local", UserRoles.Owner);
        var controller = ControllerAs(database, owner.Id, UserRoles.Owner);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            controller.BanUser(otherOwner.Id, new BanUserDto { Reason = "rules" }));

        Assert.Equal(403, exception.StatusCode);
        Assert.Equal("FORBIDDEN", exception.Code);
    }

    [Fact]
    public async Task BanUser_WhenOwnerBansRegularUser_RevokesRefreshTokensAndBumpsTokenVersion()
    {
        await using var database = await TestDatabase.CreateAsync();
        var owner = await database.AddUserAsync("owner@eatopia.local", UserRoles.Owner);
        var target = await database.AddUserAsync("user@eatopia.local", UserRoles.User);
        database.Context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = target.Id,
            TokenHash = "active-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        });
        await database.Context.SaveChangesAsync();
        var controller = ControllerAs(database, owner.Id, UserRoles.Owner);

        await controller.BanUser(target.Id, new BanUserDto { Reason = "Community rules violation" });

        database.Context.ChangeTracker.Clear();
        var reloaded = await database.Context.Users.SingleAsync(x => x.Id == target.Id);
        var token = await database.Context.RefreshTokens.SingleAsync(x => x.UserId == target.Id);
        Assert.True(reloaded.IsBanned);
        Assert.Equal("Community rules violation", reloaded.BannedReason);
        Assert.Equal(1, reloaded.JwtTokenVersion);
        Assert.NotNull(token.RevokedAt);
    }

    private static AdminController ControllerAs(TestDatabase database, Guid userId, params string[] roles)
    {
        return new AdminController(database.Context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = TestClaims.Principal(userId, roles)
                }
            }
        };
    }
}

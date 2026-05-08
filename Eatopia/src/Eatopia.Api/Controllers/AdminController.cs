using Eatopia.Application.Exceptions;
using Eatopia.Domain.Auth;
using Eatopia.Domain.Entities;
using Eatopia.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Eatopia.Api.Controllers;

[Route("api/admin")]
[Route("api/v1/admin")]
[ApiController]
[Authorize(Roles = UserRoles.Elevated)]
public class AdminController : ControllerBase
{
    private readonly EatopiaDbContext _context;

    public AdminController(EatopiaDbContext context)
    {
        _context = context;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var now = DateTime.UtcNow;
        var stats = new
        {
            users = await _context.Users.CountAsync(),
            posts = await _context.CommunityPosts.CountAsync(x => !x.IsDeleted),
            messages = await _context.ChatMessages.CountAsync(),
            activeUsers = await _context.Users.CountAsync(x => x.LastSeenAt != null && x.LastSeenAt >= now.AddMinutes(-10)),
            pendingReports = await _context.ContentReports.CountAsync(x => x.Status == "Pending"),
            bannedUsers = await _context.Users.CountAsync(x => x.IsBanned),
            recipes = await _context.Recipes.CountAsync()
        };

        return Ok(new { success = true, stats, data = stats });
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim();
            query = query.Where(x => x.Name.Contains(q) || x.Email.Contains(q) || (x.Username != null && x.Username.Contains(q)));
        }

        var total = await query.CountAsync();
        var users = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Username,
                x.Email,
                x.Role,
                x.ProfileImageUrl,
                x.IsBanned,
                x.BannedAt,
                x.BannedReason,
                x.EmailConfirmed,
                x.LastSeenAt,
                x.CreatedAt
            })
            .ToListAsync();

        return Ok(new { success = true, users, data = users, meta = new { page, pageSize, total } });
    }

    [HttpPut("users/{id:guid}/role")]
    public async Task<IActionResult> UpdateUserRole(Guid id, [FromBody] UpdateUserRoleDto dto)
    {
        if (!User.IsInRole(UserRoles.Owner))
            throw new ApiException("Only the owner can change admin permissions.", 403, "FORBIDDEN");

        var nextRole = UserRoles.Normalize(dto.Role);
        if (nextRole is null)
            throw new ApiException("Role must be Owner, Admin, or User.", 400, "VALIDATION_ERROR");

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new ApiException("User not found", 404, "NOT_FOUND");

        if (user.Role == UserRoles.Owner && nextRole != UserRoles.Owner)
        {
            var otherOwnersExist = await _context.Users.AnyAsync(x => x.Id != id && x.Role == UserRoles.Owner);
            if (!otherOwnersExist)
                throw new ApiException("At least one owner must remain in the system.", 400, "VALIDATION_ERROR");
        }

        user.Role = nextRole;
        user.JwtTokenVersion += 1;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "User role updated.", role = user.Role });
    }

    [HttpGet("reports")]
    public async Task<IActionResult> GetReports([FromQuery] string status = "Pending")
    {
        var query = _context.ContentReports.AsNoTracking().Include(x => x.Reporter).Include(x => x.ReportedUser).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
            query = query.Where(x => x.Status == status);

        var rows = await query.OrderByDescending(x => x.CreatedAt).Take(200).ToListAsync();
        var reports = new List<object>();
        foreach (var report in rows)
        {
            reports.Add(new
            {
                report.Id,
                report.ContentType,
                report.ContentId,
                report.Reason,
                report.Status,
                report.CreatedAt,
                report.ReviewedAt,
                Reporter = ToAdminUser(report.Reporter),
                ReportedUser = ToAdminUser(report.ReportedUser),
                Content = await BuildReportContentAsync(report)
            });
        }

        return Ok(new { success = true, reports, data = reports });
    }

    [HttpPut("reports/{id:guid}/status")]
    public async Task<IActionResult> UpdateReportStatus(Guid id, [FromBody] UpdateReportStatusDto dto)
    {
        var report = await _context.ContentReports.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new ApiException("Report not found", 404, "NOT_FOUND");

        var status = string.IsNullOrWhiteSpace(dto.Status) ? "Reviewed" : dto.Status.Trim();
        if (!new[] { "Pending", "Reviewed", "Dismissed", "Actioned" }.Contains(status))
            throw new ApiException("Invalid report status", 400, "VALIDATION_ERROR");

        report.Status = status;
        report.ReviewedAt = DateTime.UtcNow;
        report.ReviewedByUserId = GetCurrentUserId();
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Report updated." });
    }

    [HttpPost("reports/{id:guid}/action")]
    public async Task<IActionResult> ApplyReportAction(Guid id, [FromBody] ReportActionDto dto)
    {
        var report = await _context.ContentReports.Include(x => x.ReportedUser).FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new ApiException("Report not found", 404, "NOT_FOUND");

        var action = (dto.Action ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(action))
            throw new ApiException("Action is required.", 400, "VALIDATION_ERROR");

        var moderatorId = GetCurrentUserId();
        var now = DateTime.UtcNow;
        var message = "Report action applied.";

        switch (action)
        {
            case "reviewed":
            case "review":
                report.Status = "Reviewed";
                message = "Report marked as reviewed.";
                break;

            case "dismissed":
            case "dismiss":
                report.Status = "Dismissed";
                message = "Report dismissed.";
                break;

            case "delete-content":
            case "remove-content":
                await DeleteReportedContentAsync(report);
                report.Status = "Actioned";
                message = "Reported content removed.";
                break;

            case "warn-user":
            case "warn":
                await WarnReportedUserAsync(report, moderatorId, dto.Note);
                report.Status = "Actioned";
                message = "Warning sent to reported user.";
                break;

            case "ban-user":
            case "ban":
                if (!report.ReportedUserId.HasValue)
                    throw new ApiException("This report has no reported user.", 400, "VALIDATION_ERROR");
                await SetUserBanAsync(report.ReportedUserId.Value, true, dto.Note ?? report.Reason, moderatorId);
                report.Status = "Actioned";
                message = "Reported user banned.";
                break;

            default:
                throw new ApiException("Invalid report action.", 400, "VALIDATION_ERROR");
        }

        report.ReviewedAt = now;
        report.ReviewedByUserId = moderatorId;
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message });
    }

    [HttpPut("users/{id:guid}/ban")]
    public async Task<IActionResult> BanUser(Guid id, [FromBody] BanUserDto dto)
    {
        await SetUserBanAsync(id, true, dto.Reason, GetCurrentUserId());
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "User banned." });
    }

    [HttpPut("users/{id:guid}/unban")]
    public async Task<IActionResult> UnbanUser(Guid id)
    {
        await SetUserBanAsync(id, false, null, GetCurrentUserId());
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "User unbanned." });
    }

    [HttpDelete("posts/{id:guid}")]
    public async Task<IActionResult> DeletePost(Guid id)
    {
        var post = await _context.CommunityPosts.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw new ApiException("Post not found", 404, "NOT_FOUND");

        post.IsDeleted = true;
        post.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Post removed by admin." });
    }

    [HttpDelete("comments/{id:guid}")]
    public async Task<IActionResult> DeleteComment(Guid id)
    {
        var comment = await _context.Comments.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new ApiException("Comment not found", 404, "NOT_FOUND");

        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Comment removed by admin." });
    }

    private Guid GetCurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<object?> BuildReportContentAsync(ContentReport report)
    {
        if (string.Equals(report.ContentType, "Post", StringComparison.OrdinalIgnoreCase))
        {
            var post = await _context.CommunityPosts.AsNoTracking().Include(x => x.User).FirstOrDefaultAsync(x => x.Id == report.ContentId);
            return post == null ? null : new
            {
                kind = "Post",
                preview = post.IsDeleted ? "This post is already removed." : post.Content,
                imageUrl = post.ImageUrl,
                isDeleted = post.IsDeleted,
                author = ToAdminUser(post.User),
                targetUrl = $"/communityProfile?userId={post.UserId}"
            };
        }

        if (string.Equals(report.ContentType, "Comment", StringComparison.OrdinalIgnoreCase))
        {
            var comment = await _context.Comments.AsNoTracking().Include(x => x.User).FirstOrDefaultAsync(x => x.Id == report.ContentId);
            return comment == null ? null : new
            {
                kind = "Comment",
                preview = comment.Text,
                imageUrl = (string?)null,
                isDeleted = false,
                author = ToAdminUser(comment.User),
                targetUrl = $"/communityProfile?userId={comment.UserId}"
            };
        }

        if (string.Equals(report.ContentType, "Message", StringComparison.OrdinalIgnoreCase))
        {
            var message = await _context.ChatMessages.AsNoTracking().Include(x => x.Sender).FirstOrDefaultAsync(x => x.Id == report.ContentId);
            return message == null ? null : new
            {
                kind = "Message",
                preview = message.IsDeleted ? "This message is already removed." : message.MessageText,
                imageUrl = message.MessageType == "image" ? message.MediaContent : null,
                isDeleted = message.IsDeleted,
                author = ToAdminUser(message.Sender),
                targetUrl = $"/communityProfile?userId={message.SenderId}"
            };
        }

        if (string.Equals(report.ContentType, "User", StringComparison.OrdinalIgnoreCase))
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == report.ContentId);
            return user == null ? null : new
            {
                kind = "User",
                preview = $"{user.Name} profile report",
                imageUrl = user.ProfileImageUrl,
                isDeleted = false,
                author = ToAdminUser(user),
                targetUrl = $"/communityProfile?userId={user.Id}"
            };
        }

        return null;
    }

    private async Task DeleteReportedContentAsync(ContentReport report)
    {
        if (string.Equals(report.ContentType, "Post", StringComparison.OrdinalIgnoreCase))
        {
            var post = await _context.CommunityPosts.FirstOrDefaultAsync(x => x.Id == report.ContentId);
            if (post != null)
            {
                post.IsDeleted = true;
                post.DeletedAt = DateTime.UtcNow;
            }
            return;
        }

        if (string.Equals(report.ContentType, "Comment", StringComparison.OrdinalIgnoreCase))
        {
            var comment = await _context.Comments.FirstOrDefaultAsync(x => x.Id == report.ContentId);
            if (comment != null) _context.Comments.Remove(comment);
            return;
        }

        if (string.Equals(report.ContentType, "Message", StringComparison.OrdinalIgnoreCase))
        {
            var message = await _context.ChatMessages.FirstOrDefaultAsync(x => x.Id == report.ContentId);
            if (message != null)
            {
                message.IsDeleted = true;
                message.MessageText = "This message was removed by admin";
                message.MediaContent = null;
                message.FileName = null;
                message.DeletedAt = DateTime.UtcNow;
            }
        }
    }

    private async Task WarnReportedUserAsync(ContentReport report, Guid moderatorId, string? note)
    {
        if (!report.ReportedUserId.HasValue)
            throw new ApiException("This report has no reported user.", 400, "VALIDATION_ERROR");

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == report.ReportedUserId.Value)
            ?? throw new ApiException("Reported user not found", 404, "NOT_FOUND");

        _context.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ActorUserId = moderatorId,
            Title = "Community warning",
            Message = string.IsNullOrWhiteSpace(note) ? "An admin reviewed a report about your activity. Please follow the community rules." : note.Trim(),
            Type = "moderation_warning",
            RelatedEntityType = "Report",
            RelatedEntityId = report.Id,
            CreatedAt = DateTime.UtcNow
        });
    }

    private async Task SetUserBanAsync(Guid targetUserId, bool banned, string? reason, Guid actorId)
    {
        if (targetUserId == actorId)
            throw new ApiException("You cannot change your own ban state.", 400, "VALIDATION_ERROR");

        var target = await _context.Users.FirstOrDefaultAsync(x => x.Id == targetUserId)
            ?? throw new ApiException("User not found", 404, "NOT_FOUND");

        EnsureCanModerateUser(target);

        target.IsBanned = banned;
        target.BannedAt = banned ? DateTime.UtcNow : null;
        target.BannedReason = banned ? string.IsNullOrWhiteSpace(reason) ? "Moderation action" : reason.Trim() : null;
        target.JwtTokenVersion += 1;

        if (banned)
        {
            var now = DateTime.UtcNow;
            await _context.RefreshTokens
                .Where(x => x.UserId == targetUserId && x.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, now));
        }
    }

    private void EnsureCanModerateUser(User target)
    {
        if (target.Role == UserRoles.Owner)
            throw new ApiException("Owner accounts cannot be banned from here.", 403, "FORBIDDEN");

        if (User.IsInRole(UserRoles.Admin) && target.Role != UserRoles.User)
            throw new ApiException("Admins can only moderate regular users.", 403, "FORBIDDEN");
    }

    private static object? ToAdminUser(User? user) => user == null ? null : new
    {
        user.Id,
        user.Name,
        fullName = user.Name,
        user.Username,
        user.Email,
        user.Role,
        avatar = user.ProfileImageUrl,
        profileImage = user.ProfileImageUrl,
        user.IsBanned,
        user.BannedAt,
        user.BannedReason
    };
}

public class UpdateReportStatusDto
{
    public string Status { get; set; } = "Reviewed";
}

public class UpdateUserRoleDto
{
    public string Role { get; set; } = UserRoles.User;
}

public class ReportActionDto
{
    public string Action { get; set; } = "reviewed";
    public string? Note { get; set; }
}

public class BanUserDto
{
    public string? Reason { get; set; }
}

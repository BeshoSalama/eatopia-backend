using Eatopia.Application.Common;
using Eatopia.Application.DTOs.Community;
using Eatopia.Application.DTOs.Chat;
using Eatopia.Application.Exceptions;
using Eatopia.Domain.Entities;
using Eatopia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Eatopia.Infrastructure.Services;

public class CommunityService
{
    private readonly EatopiaDbContext _context;

    public CommunityService(EatopiaDbContext context)
    {
        _context = context;
    }

    public async Task TouchPresenceAsync(Guid userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null) return;
        user.LastSeenAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<PagedResult<object>> GetPostsAsync(Guid currentUserId, int pageIndex, int pageSize)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);
        var hiddenPostIds = await _context.HiddenPosts.AsNoTracking().Where(x => x.UserId == currentUserId).Select(x => x.PostId).ToListAsync();
        var blockedIds = await GetBlockedEitherDirectionIdsAsync(currentUserId);
        var friendIds = await GetFriendIdsAsync(currentUserId);

        var query = _context.CommunityPosts
            .AsNoTracking()
            .Where(x => !x.IsDeleted && !hiddenPostIds.Contains(x.Id) && !blockedIds.Contains(x.UserId))
            .Where(x => x.UserId == currentUserId || x.User.PostsVisibility == "Public" || (x.User.PostsVisibility == "Friends" && friendIds.Contains(x.UserId)))
            .Include(x => x.User)
            .Include(x => x.SharedPost)!.ThenInclude(x => x!.User)
            .OrderByDescending(x => x.CreatedAt);

        var paged = await query.ToPagedResultAsync(pageIndex, pageSize);
        var postIds = paged.Items.Select(x => x.Id).ToList();
        var authorIds = paged.Items.Select(x => x.UserId).Distinct().ToList();
        var comments = await _context.Comments.Include(x => x.User).Where(x => postIds.Contains(x.PostId)).OrderBy(x => x.CreatedAt).ToListAsync();
        var likes = await _context.PostLikes.Include(x => x.User).Where(x => postIds.Contains(x.PostId)).ToListAsync();
        var followed = await _context.UserFollows.AsNoTracking().Where(x => x.FollowerId == currentUserId && authorIds.Contains(x.FollowingId)).Select(x => x.FollowingId).ToListAsync();
        var items = paged.Items.Select(p => ToFrontendPost(p, comments, likes, currentUserId, followed.ToHashSet())).ToList();
        return new PagedResult<object> { PageIndex = paged.PageIndex, PageSize = paged.PageSize, TotalCount = paged.TotalCount, Items = items };
    }

    public async Task<object> GetUserCommunityProfileAsync(Guid currentUserId, Guid profileUserId)
    {
        if (currentUserId != profileUserId && await IsBlockedPairAsync(currentUserId, profileUserId))
            throw new ApiException("This profile is not available.", 403, "PROFILE_BLOCKED");

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == profileUserId) ?? throw new ApiException("User not found", 404, "USER_NOT_FOUND");
        var followersCount = await _context.UserFollows.CountAsync(x => x.FollowingId == profileUserId);
        var followingCount = await _context.UserFollows.CountAsync(x => x.FollowerId == profileUserId);
        var isFollowing = await _context.UserFollows.AnyAsync(x => x.FollowerId == currentUserId && x.FollowingId == profileUserId);
        var followsMe = await _context.UserFollows.AnyAsync(x => x.FollowerId == profileUserId && x.FollowingId == currentUserId);
        var isFriend = isFollowing && followsMe;
        if (currentUserId != profileUserId && !CanViewProfile(user, isFriend))
            throw new ApiException("This profile is private.", 403, "PROFILE_PRIVATE");
        var posts = await GetUserPostsAsync(currentUserId, profileUserId);
        return new
        {
            user = ToFrontendUser(user, isFollowing, followsMe, followersCount, followingCount),
            profile = new
            {
                id = user.Id, fullName = user.Name, name = !string.IsNullOrWhiteSpace(user.Username) ? user.Username : user.Name,
                username = user.Username, email = user.Email, bio = BuildBio(user), location = user.Location,
                avatar = Avatar(user), profileImage = user.ProfileImageUrl, followers = followersCount, following = followingCount,
                postsCount = posts.Count, isMine = currentUserId == profileUserId, isFollowing, followsMe, isFriend,
                online = user.ShowOnlineStatus && IsOnline(user), activeNow = user.ShowOnlineStatus && IsOnline(user), lastSeen = user.ShowLastSeen ? FormatLastSeen(user.LastSeenAt) : "hidden", lastSeenAt = user.ShowLastSeen ? AsUtc(user.LastSeenAt) : null
            },
            posts
        };
    }

    public async Task<List<object>> SearchUsersAsync(Guid currentUserId, string? search, bool friendsOnly = false)
    {
        var followingIds = await _context.UserFollows.AsNoTracking().Where(f => f.FollowerId == currentUserId).Select(f => f.FollowingId).ToListAsync();
        var followerIds = await _context.UserFollows.AsNoTracking().Where(f => f.FollowingId == currentUserId).Select(f => f.FollowerId).ToListAsync();
        var mutualIds = followingIds.Intersect(followerIds).ToHashSet();
        var followingSet = followingIds.ToHashSet();
        var followerSet = followerIds.ToHashSet();

        var blockedIds = await GetBlockedEitherDirectionIdsAsync(currentUserId);
        var query = _context.Users.AsNoTracking().Where(x => x.Id != currentUserId && !blockedIds.Contains(x.Id));
        if (friendsOnly) query = query.Where(x => mutualIds.Contains(x.Id));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim();
            query = query.Where(x => x.Name.Contains(q) || (x.AllowSearchByEmail && x.Email.Contains(q)) || (x.Username != null && x.Username.Contains(q)));
        }

        var users = await query.OrderBy(x => x.Name).Take(50).ToListAsync();
        var ids = users.Select(x => x.Id).ToList();
        var followerCounts = await _context.UserFollows.AsNoTracking().Where(x => ids.Contains(x.FollowingId)).GroupBy(x => x.FollowingId).Select(g => new { UserId = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.UserId, x => x.Count);
        var followingCounts = await _context.UserFollows.AsNoTracking().Where(x => ids.Contains(x.FollowerId)).GroupBy(x => x.FollowerId).Select(g => new { UserId = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.UserId, x => x.Count);
        return users.Select(u => ToFrontendUser(u, followingSet.Contains(u.Id), followerSet.Contains(u.Id), followerCounts.GetValueOrDefault(u.Id), followingCounts.GetValueOrDefault(u.Id))).ToList<object>();
    }

    public async Task<List<object>> GetFollowersAsync(Guid currentUserId, Guid? userId = null)
    {
        var targetId = userId ?? currentUserId;
        var users = await _context.UserFollows.AsNoTracking().Include(x => x.Follower).Where(x => x.FollowingId == targetId).Select(x => x.Follower).OrderBy(x => x.Name).ToListAsync();
        return await MapUsersWithRelationship(currentUserId, users);
    }

    public async Task<List<object>> GetFollowingAsync(Guid currentUserId, Guid? userId = null)
    {
        var targetId = userId ?? currentUserId;
        var users = await _context.UserFollows.AsNoTracking().Include(x => x.Following).Where(x => x.FollowerId == targetId).Select(x => x.Following).OrderBy(x => x.Name).ToListAsync();
        return await MapUsersWithRelationship(currentUserId, users);
    }

    public async Task<object> FollowUserAsync(Guid currentUserId, Guid targetUserId)
    {
        if (currentUserId == targetUserId) throw new ApiException("You cannot follow yourself", 400, "VALIDATION_ERROR");
        if (!await _context.Users.AnyAsync(x => x.Id == targetUserId)) throw new ApiException("User not found", 404, "USER_NOT_FOUND");
        if (await IsBlockedPairAsync(currentUserId, targetUserId)) throw new ApiException("You cannot follow this user.", 403, "USER_BLOCKED");
        if (!await _context.UserFollows.AnyAsync(x => x.FollowerId == currentUserId && x.FollowingId == targetUserId))
        {
            _context.UserFollows.Add(new UserFollow { Id = Guid.NewGuid(), FollowerId = currentUserId, FollowingId = targetUserId, CreatedAt = DateTime.UtcNow });
            var followsBack = await _context.UserFollows.AnyAsync(x => x.FollowerId == targetUserId && x.FollowingId == currentUserId);
            var actor = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == currentUserId);
            var actorName = DisplayName(actor);
            await AddNotificationAsync(
                targetUserId,
                followsBack ? $"{actorName} followed you back" : $"{actorName} followed you",
                followsBack ? $"{actorName} and you now follow each other." : $"{actorName} started following your profile.",
                followsBack ? "follow_back" : "follow",
                currentUserId,
                currentUserId,
                "CommunityUser",
                $"/communityProfile?userId={currentUserId}");
            await _context.SaveChangesAsync();
        }
        return await GetUserCommunityProfileAsync(currentUserId, targetUserId);
    }

    public async Task<object> UnfollowUserAsync(Guid currentUserId, Guid targetUserId)
    {
        var follow = await _context.UserFollows.FirstOrDefaultAsync(x => x.FollowerId == currentUserId && x.FollowingId == targetUserId);
        if (follow != null) { _context.UserFollows.Remove(follow); await _context.SaveChangesAsync(); }
        return await GetUserCommunityProfileAsync(currentUserId, targetUserId);
    }

    public async Task<object> CreatePostAsync(Guid userId, CreatePostDto dto)
    {
        var content = dto.Content?.Trim() ?? string.Empty;
        var image = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim();
        if (string.IsNullOrWhiteSpace(content) && string.IsNullOrWhiteSpace(image)) throw new ApiException("Write text or upload an image first", 400, "VALIDATION_ERROR");
        var post = new CommunityPost { Id = Guid.NewGuid(), UserId = userId, Content = content, ImageUrl = image, CreatedAt = DateTime.UtcNow };
        _context.CommunityPosts.Add(post);
        await _context.SaveChangesAsync();
        return await GetSinglePostAsync(userId, post.Id);
    }

    public async Task<object> UpdatePostAsync(Guid userId, Guid postId, CreatePostDto dto)
    {
        var post = await _context.CommunityPosts.FirstOrDefaultAsync(x => x.Id == postId && !x.IsDeleted) ?? throw new ApiException("Post not found", 404, "NOT_FOUND");
        if (post.UserId != userId) throw new ApiException("You can only update your own posts", 403, "FORBIDDEN");
        var content = dto.Content?.Trim() ?? string.Empty;
        var image = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim();
        if (string.IsNullOrWhiteSpace(content) && string.IsNullOrWhiteSpace(image) && post.SharedPostId == null) throw new ApiException("Post cannot be empty", 400, "VALIDATION_ERROR");
        post.Content = content; post.ImageUrl = image;
        await _context.SaveChangesAsync();
        return await GetSinglePostAsync(userId, post.Id);
    }

    public async Task DeletePostAsync(Guid userId, Guid postId)
    {
        var post = await _context.CommunityPosts.FirstOrDefaultAsync(x => x.Id == postId && !x.IsDeleted) ?? throw new ApiException("Post not found", 404, "NOT_FOUND");
        if (post.UserId != userId) throw new ApiException("You can only delete your own posts", 403, "FORBIDDEN");

        // Soft delete keeps shared-post references valid and avoids FK delete failures.
        post.IsDeleted = true;
        post.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<object> SharePostToProfileAsync(Guid userId, Guid originalPostId, SharePostToProfileDto dto)
    {
        var original = await _context.CommunityPosts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == originalPostId && !x.IsDeleted) ?? throw new ApiException("Original post not found", 404, "NOT_FOUND");
        var post = new CommunityPost { Id = Guid.NewGuid(), UserId = userId, Content = dto.Caption?.Trim() ?? string.Empty, SharedPostId = original.Id, CreatedAt = DateTime.UtcNow };
        _context.CommunityPosts.Add(post);
        await _context.SaveChangesAsync();
        return await GetSinglePostAsync(userId, post.Id);
    }

    public async Task<object> SharePostAsMessageAsync(Guid userId, Guid originalPostId, SharePostToMessageDto dto)
    {
        if (userId == dto.TargetUserId) throw new ApiException("You cannot send a post to yourself", 400, "VALIDATION_ERROR");
        var original = await _context.CommunityPosts.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == originalPostId && !x.IsDeleted) ?? throw new ApiException("Original post not found", 404, "NOT_FOUND");
        if (!await _context.Users.AnyAsync(x => x.Id == dto.TargetUserId)) throw new ApiException("User not found", 404, "USER_NOT_FOUND");
        if (await IsBlockedPairAsync(userId, dto.TargetUserId)) throw new ApiException("You cannot message this user.", 403, "USER_BLOCKED");
        var threadId = await CreateOrGetThreadIdAsync(userId, dto.TargetUserId);
        var payload = new
        {
            postId = original.Id,
            text = original.Content,
            imageUrl = original.ImageUrl,
            author = original.User == null ? null : ToFrontendUser(original.User, false, false, 0, 0),
            createdAt = AsUtc(original.CreatedAt)
        };
        var now = DateTime.UtcNow;
        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ThreadId = threadId,
            SenderId = userId,
            MessageText = string.IsNullOrWhiteSpace(dto.Message) ? "Shared a post" : dto.Message.Trim(),
            MessageType = "post",
            MediaContent = JsonSerializer.Serialize(payload),
            SentAt = now,
            DeliveredAt = now,
            IsDeleted = false,
            CreatedAt = now
        };
        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();
        return new { threadId, messageId = message.Id };
    }

    public async Task<object> AddCommentAsync(Guid userId, Guid postId, CreateCommentDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Text)) throw new ApiException("Text is required", 400, "VALIDATION_ERROR");
        if (!await _context.CommunityPosts.AnyAsync(x => x.Id == postId && !x.IsDeleted)) throw new ApiException("Post not found", 404, "NOT_FOUND");
        var comment = new Comment { Id = Guid.NewGuid(), PostId = postId, UserId = userId, Text = dto.Text.Trim(), CreatedAt = DateTime.UtcNow };
        _context.Comments.Add(comment);
        var postAuthorId = await _context.CommunityPosts.Where(x => x.Id == postId).Select(x => x.UserId).FirstAsync();
        if (postAuthorId != userId)
        {
            var actor = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
            var actorName = DisplayName(actor);
            await AddNotificationAsync(postAuthorId, $"{actorName} commented", $"{actorName} commented on your post.", "comment", postId, userId, "CommunityPost", $"/communityProfile?userId={userId}");
        }
        await _context.SaveChangesAsync();
        return await GetSingleCommentAsync(userId, comment.Id);
    }

    public async Task<object> UpdateCommentAsync(Guid userId, Guid commentId, CreateCommentDto dto)
    {
        var comment = await _context.Comments.FirstOrDefaultAsync(x => x.Id == commentId) ?? throw new ApiException("Comment not found", 404, "NOT_FOUND");
        if (comment.UserId != userId) throw new ApiException("You can only update your own comments", 403, "FORBIDDEN");
        if (string.IsNullOrWhiteSpace(dto.Text)) throw new ApiException("Text is required", 400, "VALIDATION_ERROR");
        comment.Text = dto.Text.Trim(); await _context.SaveChangesAsync(); return await GetSingleCommentAsync(userId, commentId);
    }

    public async Task DeleteCommentAsync(Guid userId, Guid commentId)
    {
        var comment = await _context.Comments.FirstOrDefaultAsync(x => x.Id == commentId) ?? throw new ApiException("Comment not found", 404, "NOT_FOUND");
        if (comment.UserId != userId) throw new ApiException("You can only delete your own comments", 403, "FORBIDDEN");
        _context.Comments.Remove(comment); await _context.SaveChangesAsync();
    }

    public async Task<PagedResult<object>> GetCommentsAsync(Guid postId, int pageIndex, int pageSize)
    {
        if (!await _context.CommunityPosts.AnyAsync(x => x.Id == postId && !x.IsDeleted)) throw new ApiException("Post not found", 404, "NOT_FOUND");
        var paged = await _context.Comments.Include(x => x.User).Where(x => x.PostId == postId).OrderBy(x => x.CreatedAt).ToPagedResultAsync(pageIndex, pageSize);
        return new PagedResult<object> { PageIndex = paged.PageIndex, PageSize = paged.PageSize, TotalCount = paged.TotalCount, Items = paged.Items.Select(c => ToFrontendComment(c, Guid.Empty)).ToList() };
    }

    public async Task LikePostAsync(Guid userId, Guid postId)
    {
        if (!await _context.CommunityPosts.AnyAsync(x => x.Id == postId && !x.IsDeleted)) throw new ApiException("Post not found", 404, "NOT_FOUND");
        if (await _context.PostLikes.AnyAsync(x => x.PostId == postId && x.UserId == userId)) return;
        _context.PostLikes.Add(new PostLike { Id = Guid.NewGuid(), PostId = postId, UserId = userId, CreatedAt = DateTime.UtcNow });
        var postAuthorId = await _context.CommunityPosts.Where(x => x.Id == postId).Select(x => x.UserId).FirstAsync();
        if (postAuthorId != userId)
        {
            var actor = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
            var actorName = DisplayName(actor);
            await AddNotificationAsync(postAuthorId, $"{actorName} liked your post", $"{actorName} reacted to your community post.", "post_like", postId, userId, "CommunityPost", $"/communityProfile?userId={userId}");
        }
        await _context.SaveChangesAsync();
    }

    public async Task UnlikePostAsync(Guid userId, Guid postId)
    {
        var like = await _context.PostLikes.FirstOrDefaultAsync(x => x.PostId == postId && x.UserId == userId);
        if (like == null) return; _context.PostLikes.Remove(like); await _context.SaveChangesAsync();
    }


    public async Task HidePostAsync(Guid userId, Guid postId)
    {
        if (!await _context.CommunityPosts.AnyAsync(x => x.Id == postId && !x.IsDeleted))
            throw new ApiException("Post not found", 404, "NOT_FOUND");

        if (!await _context.HiddenPosts.AnyAsync(x => x.UserId == userId && x.PostId == postId))
        {
            _context.HiddenPosts.Add(new HiddenPost { Id = Guid.NewGuid(), UserId = userId, PostId = postId, CreatedAt = DateTime.UtcNow });
            await _context.SaveChangesAsync();
        }
    }

    public async Task ReportPostAsync(Guid userId, Guid postId, string reason)
    {
        var post = await _context.CommunityPosts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == postId && !x.IsDeleted)
            ?? throw new ApiException("Post not found", 404, "NOT_FOUND");
        await CreateReportAsync(userId, "Post", postId, post.UserId, reason);
    }

    public async Task ReportCommentAsync(Guid userId, Guid commentId, string reason)
    {
        var comment = await _context.Comments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == commentId)
            ?? throw new ApiException("Comment not found", 404, "NOT_FOUND");
        await CreateReportAsync(userId, "Comment", commentId, comment.UserId, reason);
    }

    public async Task ReportMessageAsync(Guid userId, Guid messageId, string reason)
    {
        var message = await _context.ChatMessages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == messageId && !x.IsDeleted)
            ?? throw new ApiException("Message not found", 404, "NOT_FOUND");

        if (!await _context.ChatParticipants.AnyAsync(x => x.ThreadId == message.ThreadId && x.UserId == userId))
            throw new ApiException("Forbidden", 403, "FORBIDDEN");

        await CreateReportAsync(userId, "Message", messageId, message.SenderId, reason);
    }

    public async Task ReportUserAsync(Guid userId, Guid targetUserId, string reason)
    {
        if (userId == targetUserId)
            throw new ApiException("You cannot report yourself.", 400, "VALIDATION_ERROR");

        if (!await _context.Users.AsNoTracking().AnyAsync(x => x.Id == targetUserId))
            throw new ApiException("User not found", 404, "USER_NOT_FOUND");

        await CreateReportAsync(userId, "User", targetUserId, targetUserId, reason);
    }

    public async Task<object> BlockUserAsync(Guid currentUserId, Guid targetUserId)
    {
        if (currentUserId == targetUserId) throw new ApiException("You cannot block yourself", 400, "VALIDATION_ERROR");
        if (!await _context.Users.AnyAsync(x => x.Id == targetUserId)) throw new ApiException("User not found", 404, "USER_NOT_FOUND");

        if (!await _context.UserBlocks.AnyAsync(x => x.BlockerId == currentUserId && x.BlockedId == targetUserId))
            _context.UserBlocks.Add(new UserBlock { Id = Guid.NewGuid(), BlockerId = currentUserId, BlockedId = targetUserId, CreatedAt = DateTime.UtcNow });

        await _context.UserFollows.Where(x => (x.FollowerId == currentUserId && x.FollowingId == targetUserId) || (x.FollowerId == targetUserId && x.FollowingId == currentUserId)).ExecuteDeleteAsync();

        var sharedThreadIds = await _context.ChatParticipants
            .Where(p => p.UserId == currentUserId)
            .Select(p => p.ThreadId)
            .Intersect(_context.ChatParticipants.Where(p => p.UserId == targetUserId).Select(p => p.ThreadId))
            .ToListAsync();

        if (sharedThreadIds.Count > 0)
            await _context.ChatThreads.Where(x => sharedThreadIds.Contains(x.Id)).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.RequestStatus, "Blocked").SetProperty(x => x.DeletedAt, DateTime.UtcNow));

        await _context.SaveChangesAsync();
        return new { blockedUserId = targetUserId };
    }

    public async Task UnblockUserAsync(Guid currentUserId, Guid targetUserId)
    {
        var block = await _context.UserBlocks.FirstOrDefaultAsync(x => x.BlockerId == currentUserId && x.BlockedId == targetUserId);
        if (block != null)
        {
            _context.UserBlocks.Remove(block);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<object>> GetBlockedUsersAsync(Guid currentUserId)
    {
        var rows = await _context.UserBlocks
            .AsNoTracking()
            .Include(x => x.Blocked)
            .Where(x => x.BlockerId == currentUserId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return rows.Select(x => ToFrontendUser(x.Blocked, false, false, 0, 0)).ToList<object>();
    }

    private async Task CreateReportAsync(Guid reporterId, string contentType, Guid contentId, Guid reportedUserId, string reason)
    {
        var cleanReason = (reason ?? string.Empty).Trim();
        if (cleanReason.Length < 3) throw new ApiException("Report reason is required", 400, "VALIDATION_ERROR");

        if (await _context.ContentReports.AnyAsync(x => x.ReporterId == reporterId && x.ContentType == contentType && x.ContentId == contentId))
            throw new ApiException("You already reported this item.", 409, "ALREADY_REPORTED");

        _context.ContentReports.Add(new ContentReport
        {
            Id = Guid.NewGuid(),
            ReporterId = reporterId,
            ContentType = contentType,
            ContentId = contentId,
            ReportedUserId = reportedUserId,
            Reason = cleanReason.Length > 1000 ? cleanReason[..1000] : cleanReason,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    private static bool CanViewProfile(User user, bool isFriend)
    {
        var visibility = (user.ProfileVisibility ?? "Public").Trim().ToLowerInvariant();
        return visibility switch
        {
            "private" => false,
            "friends" => isFriend,
            _ => true
        };
    }

    private static bool CanViewPosts(User user, bool isMine, bool isFriend)
    {
        if (isMine) return true;
        var visibility = (user.PostsVisibility ?? "Public").Trim().ToLowerInvariant();
        return visibility switch
        {
            "private" => false,
            "friends" => isFriend,
            _ => true
        };
    }

    private async Task<List<Guid>> GetFriendIdsAsync(Guid userId)
    {
        var followingIds = await _context.UserFollows.AsNoTracking().Where(x => x.FollowerId == userId).Select(x => x.FollowingId).ToListAsync();
        var followerIds = await _context.UserFollows.AsNoTracking().Where(x => x.FollowingId == userId).Select(x => x.FollowerId).ToListAsync();
        return followingIds.Intersect(followerIds).ToList();
    }

    private async Task<bool> IsBlockedPairAsync(Guid firstUserId, Guid secondUserId)
    {
        return await _context.UserBlocks.AnyAsync(x =>
            (x.BlockerId == firstUserId && x.BlockedId == secondUserId) ||
            (x.BlockerId == secondUserId && x.BlockedId == firstUserId));
    }

    private async Task<HashSet<Guid>> GetBlockedEitherDirectionIdsAsync(Guid userId)
    {
        var rows = await _context.UserBlocks.AsNoTracking()
            .Where(x => x.BlockerId == userId || x.BlockedId == userId)
            .Select(x => new { x.BlockerId, x.BlockedId })
            .ToListAsync();

        return rows.Select(x => x.BlockerId == userId ? x.BlockedId : x.BlockerId).ToHashSet();
    }

    private async Task AddNotificationAsync(Guid userId, string title, string message, string type, Guid relatedId, Guid? actorUserId = null, string relatedEntityType = "Community", string? actionUrl = null)
    {
        var userPrefs = await _context.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new { x.NotificationsEnabled, x.CommunityNotificationsEnabled })
            .FirstOrDefaultAsync();

        if (userPrefs == null || !userPrefs.NotificationsEnabled || !userPrefs.CommunityNotificationsEnabled)
            return;

        _context.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ActorUserId = actorUserId,
            Title = title,
            Message = message,
            Type = type,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedId,
            ActionUrl = actionUrl,
            CreatedAt = DateTime.UtcNow
        });
    }

    private async Task<List<object>> MapUsersWithRelationship(Guid currentUserId, List<User> users)
    {
        var ids = users.Select(x => x.Id).ToList();
        var following = await _context.UserFollows.AsNoTracking().Where(x => x.FollowerId == currentUserId && ids.Contains(x.FollowingId)).Select(x => x.FollowingId).ToListAsync();
        var followers = await _context.UserFollows.AsNoTracking().Where(x => x.FollowingId == currentUserId && ids.Contains(x.FollowerId)).Select(x => x.FollowerId).ToListAsync();
        var followerCounts = await _context.UserFollows.AsNoTracking().Where(x => ids.Contains(x.FollowingId)).GroupBy(x => x.FollowingId).Select(g => new { UserId = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.UserId, x => x.Count);
        var followingCounts = await _context.UserFollows.AsNoTracking().Where(x => ids.Contains(x.FollowerId)).GroupBy(x => x.FollowerId).Select(g => new { UserId = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.UserId, x => x.Count);
        var followingSet = following.ToHashSet(); var followerSet = followers.ToHashSet();
        return users.Select(u => ToFrontendUser(u, followingSet.Contains(u.Id), followerSet.Contains(u.Id), followerCounts.GetValueOrDefault(u.Id), followingCounts.GetValueOrDefault(u.Id))).ToList<object>();
    }

    private async Task<List<object>> GetUserPostsAsync(Guid currentUserId, Guid profileUserId)
    {
        var owner = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == profileUserId)
            ?? throw new ApiException("User not found", 404, "USER_NOT_FOUND");

        var isMine = currentUserId == profileUserId;
        var isFriend = isMine || (await _context.UserFollows.AnyAsync(x => x.FollowerId == currentUserId && x.FollowingId == profileUserId)
            && await _context.UserFollows.AnyAsync(x => x.FollowerId == profileUserId && x.FollowingId == currentUserId));

        if (!CanViewPosts(owner, isMine, isFriend))
            return new List<object>();

        var posts = await _context.CommunityPosts.Include(x => x.User).Include(x => x.SharedPost)!.ThenInclude(x => x!.User).Where(x => x.UserId == profileUserId && !x.IsDeleted).OrderByDescending(x => x.CreatedAt).ToListAsync();
        var postIds = posts.Select(x => x.Id).ToList();
        var comments = await _context.Comments.Include(x => x.User).Where(x => postIds.Contains(x.PostId)).OrderBy(x => x.CreatedAt).ToListAsync();
        var likes = await _context.PostLikes.Include(x => x.User).Where(x => postIds.Contains(x.PostId)).ToListAsync();
        var followed = await _context.UserFollows.AsNoTracking().Where(x => x.FollowerId == currentUserId).Select(x => x.FollowingId).ToListAsync();
        return posts.Select(p => ToFrontendPost(p, comments, likes, currentUserId, followed.ToHashSet())).ToList();
    }

    private async Task<object> GetSinglePostAsync(Guid currentUserId, Guid postId)
    {
        var post = await _context.CommunityPosts.Include(x => x.User).Include(x => x.SharedPost)!.ThenInclude(x => x!.User).FirstOrDefaultAsync(x => x.Id == postId && !x.IsDeleted) ?? throw new ApiException("Post not found", 404, "NOT_FOUND");
        var comments = await _context.Comments.Include(x => x.User).Where(x => x.PostId == postId).OrderBy(x => x.CreatedAt).ToListAsync();
        var likes = await _context.PostLikes.Include(x => x.User).Where(x => x.PostId == postId).ToListAsync();
        var followed = await _context.UserFollows.AsNoTracking().Where(x => x.FollowerId == currentUserId).Select(x => x.FollowingId).ToListAsync();
        return ToFrontendPost(post, comments, likes, currentUserId, followed.ToHashSet());
    }

    private async Task<object> GetSingleCommentAsync(Guid currentUserId, Guid commentId)
    {
        var comment = await _context.Comments.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == commentId) ?? throw new ApiException("Comment not found", 404, "NOT_FOUND");
        return ToFrontendComment(comment, currentUserId);
    }

    private async Task<Guid> CreateOrGetThreadIdAsync(Guid userId, Guid otherUserId)
    {
        var now = DateTime.UtcNow;
        var areFriends = await _context.UserFollows.AnyAsync(x => x.FollowerId == userId && x.FollowingId == otherUserId)
            && await _context.UserFollows.AnyAsync(x => x.FollowerId == otherUserId && x.FollowingId == userId);

        var existing = await _context.ChatParticipants
            .Where(p => p.UserId == userId)
            .Select(p => p.ThreadId)
            .Intersect(_context.ChatParticipants.Where(p => p.UserId == otherUserId).Select(p => p.ThreadId))
            .FirstOrDefaultAsync();

        if (existing != Guid.Empty)
        {
            var existingThread = await _context.ChatThreads.FirstOrDefaultAsync(x => x.Id == existing);
            if (existingThread == null) return existing;

            if (string.Equals(existingThread.RequestStatus, "Blocked", StringComparison.OrdinalIgnoreCase))
                throw new ApiException("This conversation is blocked.", 403, "USER_BLOCKED");

            if (areFriends)
            {
                existingThread.RequestStatus = "Accepted";
                existingThread.AcceptedAt ??= now;
                existingThread.DeletedAt = null;
            }
            else if (string.Equals(existingThread.RequestStatus, "Deleted", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(existingThread.RequestStatus))
            {
                if (!await _context.Users.AsNoTracking().Where(x => x.Id == otherUserId).Select(x => x.AllowMessageRequests).FirstOrDefaultAsync())
                    throw new ApiException("This user is not accepting message requests right now.", 403, "MESSAGE_REQUESTS_DISABLED");
                existingThread.RequestStatus = "Pending";
                existingThread.RequestedByUserId = userId;
                existingThread.DeletedAt = null;
            }
            else if (string.Equals(existingThread.RequestStatus, "Pending", StringComparison.OrdinalIgnoreCase) && existingThread.RequestedByUserId != userId)
            {
                existingThread.RequestStatus = "Accepted";
                existingThread.AcceptedAt = now;
                existingThread.DeletedAt = null;
            }

            return existing;
        }

        if (!areFriends && !await _context.Users.AsNoTracking().Where(x => x.Id == otherUserId).Select(x => x.AllowMessageRequests).FirstOrDefaultAsync())
            throw new ApiException("This user is not accepting message requests right now.", 403, "MESSAGE_REQUESTS_DISABLED");

        var thread = new ChatThread
        {
            Id = Guid.NewGuid(),
            RequestStatus = areFriends ? "Accepted" : "Pending",
            RequestedByUserId = areFriends ? null : userId,
            AcceptedAt = areFriends ? now : null,
            CreatedAt = now
        };

        _context.ChatThreads.Add(thread);
        _context.ChatParticipants.AddRange(
            new ChatParticipant { Id = Guid.NewGuid(), ThreadId = thread.Id, UserId = userId, JoinedAt = now, CreatedAt = now },
            new ChatParticipant { Id = Guid.NewGuid(), ThreadId = thread.Id, UserId = otherUserId, JoinedAt = now, CreatedAt = now });

        await _context.SaveChangesAsync();
        return thread.Id;
    }

    private static DateTime AsUtc(DateTime value) => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    private static DateTime? AsUtc(DateTime? value) => value.HasValue ? AsUtc(value.Value) : null;

    private static object ToFrontendPost(CommunityPost p, List<Comment> comments, List<PostLike> likes, Guid currentUserId, HashSet<Guid> followedAuthorIds)
    {
        var postComments = comments.Where(c => c.PostId == p.Id).ToList();
        var postLikes = likes.Where(l => l.PostId == p.Id).ToList();

        return new
        {
            id = p.Id,
            text = p.Content,
            content = p.Content,
            imageUrl = p.ImageUrl,
            createdAt = AsUtc(p.CreatedAt),
            isDeleted = p.IsDeleted,
            isMine = p.UserId == currentUserId,
            isLiked = postLikes.Any(l => l.UserId == currentUserId),
            likes = postLikes.Count,
            likedBy = postLikes.Select(l => l.User == null ? "User" : l.User.Name).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList(),
            isFromFollowedAuthor = p.UserId == currentUserId || followedAuthorIds.Contains(p.UserId),
            author = ToFrontendUser(p.User, false, false, 0, 0),
            user = ToFrontendUser(p.User, false, false, 0, 0),
            sharedPost = p.SharedPost == null ? null : ToSharedPost(p.SharedPost),
            comments = postComments.Select(c => ToFrontendComment(c, currentUserId)).ToList()
        };
    }

    private static object ToSharedPost(CommunityPost p) => p.IsDeleted
        ? new { id = p.Id, text = "This post was deleted.", content = "This post was deleted.", imageUrl = (string?)null, createdAt = AsUtc(p.CreatedAt), isDeleted = true, author = ToFrontendUser(p.User, false, false, 0, 0), user = ToFrontendUser(p.User, false, false, 0, 0) }
        : new { id = p.Id, text = p.Content, content = p.Content, imageUrl = p.ImageUrl, createdAt = AsUtc(p.CreatedAt), isDeleted = false, author = ToFrontendUser(p.User, false, false, 0, 0), user = ToFrontendUser(p.User, false, false, 0, 0) };
    private static object ToFrontendComment(Comment c, Guid currentUserId) => new { id = c.Id, text = c.Text, createdAt = AsUtc(c.CreatedAt), isMine = c.UserId == currentUserId, author = ToFrontendUser(c.User, false, false, 0, 0), user = ToFrontendUser(c.User, false, false, 0, 0) };
    private static string BuildBio(User user) { var parts = new List<string>(); if (!string.IsNullOrWhiteSpace(user.Goal)) parts.Add(user.Goal); if (!string.IsNullOrWhiteSpace(user.ActivityLevel)) parts.Add(user.ActivityLevel); if (!string.IsNullOrWhiteSpace(user.Gender)) parts.Add(user.Gender); return parts.Count == 0 ? "Eatopia community member" : string.Join(" · ", parts); }
    private static string? Avatar(User user) => string.IsNullOrWhiteSpace(user.ProfileImageUrl) ? null : user.ProfileImageUrl;
    private static string DisplayName(User? user) => user == null ? "Someone" : string.IsNullOrWhiteSpace(user.Username) ? user.Name : user.Username;
    private static bool IsOnline(User user) => user.LastSeenAt.HasValue && DateTime.UtcNow - user.LastSeenAt.Value <= TimeSpan.FromMinutes(2);
    private static string FormatLastSeen(DateTime? lastSeenAt) { if (!lastSeenAt.HasValue) return "not active yet"; var diff = DateTime.UtcNow - lastSeenAt.Value; if (diff.TotalMinutes < 1) return "just now"; if (diff.TotalMinutes < 60) return $"{Math.Floor(diff.TotalMinutes)} min ago"; if (diff.TotalHours < 24) return $"{Math.Floor(diff.TotalHours)} hours ago"; return $"{Math.Floor(diff.TotalDays)} days ago"; }
    private static object ToFrontendUser(User? user, bool isFollowing, bool followsMe, int followersCount, int followingCount) => user == null
        ? new { id = Guid.Empty, name = "User", fullName = "User", avatar = (string?)null, profileImage = (string?)null, gender = (string?)null, online = false, activeNow = false }
        : new
        {
            id = user.Id,
            name = !string.IsNullOrWhiteSpace(user.Username) ? user.Username : user.Name,
            fullName = user.Name,
            username = user.Username,
            email = user.Email,
            avatar = Avatar(user),
            profileImage = user.ProfileImageUrl,
            gender = user.Gender,
            location = user.Location,
            bio = BuildBio(user),
            followers = followersCount,
            following = followingCount,
            isFollowing,
            followsMe,
            isFriend = isFollowing && followsMe,
            online = user.ShowOnlineStatus && IsOnline(user),
            activeNow = user.ShowOnlineStatus && IsOnline(user),
            lastSeen = user.ShowLastSeen ? FormatLastSeen(user.LastSeenAt) : "hidden",
            lastSeenAt = user.ShowLastSeen ? AsUtc(user.LastSeenAt) : null
        };
}

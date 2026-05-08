using Eatopia.Application.Common;
using Eatopia.Application.DTOs.Chat;
using Eatopia.Application.Exceptions;
using Eatopia.Domain.Entities;
using Eatopia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Eatopia.Infrastructure.Services;

public class ChatService
{
    private const string RequestPending = "Pending";
    private const string RequestAccepted = "Accepted";
    private const string RequestDeleted = "Deleted";
    private const string RequestBlocked = "Blocked";

    private static readonly HashSet<string> MediaMessageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image", "video", "file", "audio", "post"
    };

    private readonly EatopiaDbContext _context;

    public ChatService(EatopiaDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> CreateOrGetThreadAsync(Guid userId, CreateThreadDto dto)
    {
        if (dto.OtherUserId == userId)
            throw new ApiException("You cannot chat with yourself", 400, "VALIDATION_ERROR");

        if (!await _context.Users.AnyAsync(x => x.Id == dto.OtherUserId))
            throw new ApiException("Other user not found", 404, "USER_NOT_FOUND");

        if (await IsBlockedPairAsync(userId, dto.OtherUserId))
            throw new ApiException("You cannot message this user.", 403, "USER_BLOCKED");

        var existing = await _context.ChatParticipants
            .Where(p => p.UserId == userId)
            .Select(p => p.ThreadId)
            .Intersect(_context.ChatParticipants.Where(p => p.UserId == dto.OtherUserId).Select(p => p.ThreadId))
            .FirstOrDefaultAsync();

        if (existing != Guid.Empty)
            return existing;

        var isFriend = await AreFriendsAsync(userId, dto.OtherUserId);
        if (!isFriend && !await AllowsMessageRequestsAsync(dto.OtherUserId))
            throw new ApiException("This user is not accepting message requests right now.", 403, "MESSAGE_REQUESTS_DISABLED");
        var now = DateTime.UtcNow;
        var thread = new ChatThread
        {
            Id = Guid.NewGuid(),
            CreatedAt = now,
            RequestStatus = isFriend ? RequestAccepted : RequestPending,
            RequestedByUserId = isFriend ? null : userId,
            AcceptedAt = isFriend ? now : null
        };

        _context.ChatThreads.Add(thread);
        _context.ChatParticipants.AddRange(
            new ChatParticipant { Id = Guid.NewGuid(), ThreadId = thread.Id, UserId = userId, JoinedAt = now, CreatedAt = now },
            new ChatParticipant { Id = Guid.NewGuid(), ThreadId = thread.Id, UserId = dto.OtherUserId, JoinedAt = now, CreatedAt = now });

        await TouchPresenceAsync(userId);
        await _context.SaveChangesAsync();

        return thread.Id;
    }

    public async Task<bool> IsUserInThreadAsync(Guid threadId, Guid userId)
    {
        return await _context.ChatParticipants.AnyAsync(x => x.ThreadId == threadId && x.UserId == userId);
    }

    public async Task<List<Guid>> GetThreadParticipantUserIdsAsync(Guid threadId)
    {
        return await _context.ChatParticipants
            .AsNoTracking()
            .Where(x => x.ThreadId == threadId)
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync();
    }

    public async Task<object> SaveMessageAsync(Guid threadId, Guid senderId, SendMessageDto dto)
    {
        var type = NormalizeMessageType(dto.MessageType);
        var text = dto.MessageText?.Trim() ?? string.Empty;
        var mediaContent = dto.MediaContent?.Trim();

        if (type == "text" && string.IsNullOrWhiteSpace(text))
            throw new ApiException("MessageText is required", 400, "VALIDATION_ERROR");

        if (MediaMessageTypes.Contains(type) && type != "post" && string.IsNullOrWhiteSpace(mediaContent))
            throw new ApiException("MediaContent is required", 400, "VALIDATION_ERROR");

        if (!string.IsNullOrWhiteSpace(mediaContent) && mediaContent.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            throw new ApiException("Upload media first, then send the returned URL.", 400, "VALIDATION_ERROR");

        if (!string.IsNullOrWhiteSpace(mediaContent) && mediaContent.Length > 2000)
            throw new ApiException("MediaContent URL is too long", 400, "VALIDATION_ERROR");

        var thread = await _context.ChatThreads.FirstOrDefaultAsync(x => x.Id == threadId)
            ?? throw new ApiException("Thread not found", 404, "NOT_FOUND");

        var participantIds = await _context.ChatParticipants
            .Where(x => x.ThreadId == threadId)
            .Select(x => x.UserId)
            .ToListAsync();

        if (!participantIds.Contains(senderId))
            throw new ApiException("Forbidden", 403, "FORBIDDEN");

        var recipientIds = participantIds.Where(x => x != senderId).ToList();
        if (recipientIds.Count == 0)
            throw new ApiException("Conversation has no recipient", 400, "VALIDATION_ERROR");

        foreach (var recipientId in recipientIds)
        {
            if (await IsBlockedPairAsync(senderId, recipientId))
                throw new ApiException("You cannot message this user.", 403, "USER_BLOCKED");
        }

        var otherUserId = recipientIds.First();
        var isFriend = await AreFriendsAsync(senderId, otherUserId);
        if (!isFriend && !string.Equals(thread.RequestStatus, RequestAccepted, StringComparison.OrdinalIgnoreCase) &&
            (thread.RequestedByUserId == senderId || thread.RequestedByUserId == null || string.Equals(thread.RequestStatus, RequestDeleted, StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var recipientId in recipientIds)
            {
                if (!await AllowsMessageRequestsAsync(recipientId))
                    throw new ApiException("This user is not accepting message requests right now.", 403, "MESSAGE_REQUESTS_DISABLED");
            }
        }
        var wasPendingRequest = string.Equals(thread.RequestStatus, RequestPending, StringComparison.OrdinalIgnoreCase);
        var senderUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == senderId);
        var senderName = DisplayName(senderUser);

        if (isFriend)
        {
            thread.RequestStatus = RequestAccepted;
            thread.AcceptedAt ??= DateTime.UtcNow;
            thread.DeletedAt = null;
        }
        else
        {
            if (string.Equals(thread.RequestStatus, RequestBlocked, StringComparison.OrdinalIgnoreCase))
                throw new ApiException("This conversation is blocked.", 403, "USER_BLOCKED");

            if (string.Equals(thread.RequestStatus, RequestDeleted, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(thread.RequestStatus))
            {
                thread.RequestStatus = RequestPending;
                thread.RequestedByUserId = senderId;
                thread.DeletedAt = null;
            }
            else if (string.Equals(thread.RequestStatus, RequestPending, StringComparison.OrdinalIgnoreCase) && thread.RequestedByUserId != senderId)
            {
                // Replying to a request is an intentional accept.
                thread.RequestStatus = RequestAccepted;
                thread.AcceptedAt = DateTime.UtcNow;
                thread.DeletedAt = null;
            }
            else if (!string.Equals(thread.RequestStatus, RequestAccepted, StringComparison.OrdinalIgnoreCase))
            {
                thread.RequestStatus = RequestPending;
                thread.RequestedByUserId ??= senderId;
            }
        }

        var now = DateTime.UtcNow;
        var msg = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ThreadId = threadId,
            SenderId = senderId,
            MessageText = text,
            MessageType = type,
            MediaContent = mediaContent,
            FileName = string.IsNullOrWhiteSpace(dto.FileName) ? null : dto.FileName.Trim(),
            SentAt = now,
            IsDeleted = false,
            DeliveredAt = now,
            CreatedAt = now
        };

        _context.ChatMessages.Add(msg);
        await TouchPresenceAsync(senderId);

        if (!isFriend && thread.RequestStatus == RequestPending && thread.RequestedByUserId == senderId)
        {
            foreach (var recipientId in recipientIds)
                await AddNotificationAsync(recipientId, $"{senderName} sent a message request", $"{senderName} wants to message you.", "message_request", threadId, senderId, $"/communityProfile?userId={senderId}");
        }
        else if (!wasPendingRequest || thread.RequestStatus == RequestAccepted)
        {
            foreach (var recipientId in recipientIds)
                await AddNotificationAsync(recipientId, $"{senderName} sent a message", PreviewMessage(senderName, type, text), "message", threadId, senderId, $"/communityProfile?userId={senderId}");
        }

        await _context.SaveChangesAsync();

        return ToFrontendMessage(msg, senderId);
    }

    public async Task<object> UpdateMessageAsync(Guid threadId, Guid messageId, Guid userId, UpdateMessageDto dto)
    {
        if (!await IsUserInThreadAsync(threadId, userId))
            throw new ApiException("Forbidden", 403, "FORBIDDEN");

        var message = await _context.ChatMessages
            .FirstOrDefaultAsync(x => x.Id == messageId && x.ThreadId == threadId && !x.IsDeleted)
            ?? throw new ApiException("Message not found", 404, "NOT_FOUND");

        if (message.SenderId != userId)
            throw new ApiException("You can only edit your own messages", 403, "FORBIDDEN");

        if (!string.Equals(message.MessageType, "text", StringComparison.OrdinalIgnoreCase))
            throw new ApiException("Only text messages can be edited. Delete and resend media if needed.", 400, "VALIDATION_ERROR");

        var text = dto.MessageText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
            throw new ApiException("MessageText is required", 400, "VALIDATION_ERROR");

        message.MessageText = text;
        message.EditedAt = DateTime.UtcNow;
        await TouchPresenceAsync(userId);
        await _context.SaveChangesAsync();

        return ToFrontendMessage(message, userId);
    }

    public async Task<object> DeleteMessageAsync(Guid threadId, Guid messageId, Guid userId)
    {
        if (!await IsUserInThreadAsync(threadId, userId))
            throw new ApiException("Forbidden", 403, "FORBIDDEN");

        var message = await _context.ChatMessages
            .FirstOrDefaultAsync(x => x.Id == messageId && x.ThreadId == threadId && !x.IsDeleted)
            ?? throw new ApiException("Message not found", 404, "NOT_FOUND");

        if (message.SenderId != userId)
            throw new ApiException("You can only delete your own messages", 403, "FORBIDDEN");

        message.IsDeleted = true;
        message.MessageText = "This message was deleted";
        message.MediaContent = null;
        message.FileName = null;
        message.DeletedAt = DateTime.UtcNow;
        await TouchPresenceAsync(userId);
        await _context.SaveChangesAsync();

        return ToFrontendMessage(message, userId);
    }

    public async Task<PagedResult<object>> GetMessagesAsync(Guid threadId, Guid userId, int pageIndex, int pageSize)
    {
        if (!await IsUserInThreadAsync(threadId, userId))
            throw new ApiException("Forbidden", 403, "FORBIDDEN");

        pageSize = Math.Clamp(pageSize, 1, 100);

        var paged = await _context.ChatMessages
            .AsNoTracking()
            .Where(x => x.ThreadId == threadId)
            .OrderByDescending(x => x.SentAt)
            .ToPagedResultAsync(pageIndex, pageSize);

        return new PagedResult<object>
        {
            PageIndex = paged.PageIndex,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount,
            Items = paged.Items.OrderBy(x => x.SentAt).Select(m => ToFrontendMessage(m, userId)).ToList()
        };
    }

    public async Task MarkThreadReadAsync(Guid threadId, Guid userId)
    {
        if (!await IsUserInThreadAsync(threadId, userId))
            throw new ApiException("Forbidden", 403, "FORBIDDEN");

        var now = DateTime.UtcNow;
        await _context.ChatMessages
            .Where(x => x.ThreadId == threadId && x.SenderId != userId && !x.IsDeleted && x.SeenAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.SeenAt, now));

        await TouchPresenceAsync(userId);
        await _context.SaveChangesAsync();
    }

    public async Task<List<object>> GetUserThreadsAsync(Guid userId)
    {
        var threadIds = await _context.ChatParticipants
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.ThreadId)
            .Distinct()
            .ToListAsync();

        if (threadIds.Count == 0)
            return new List<object>();

        var otherParticipants = await _context.ChatParticipants
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => threadIds.Contains(x.ThreadId) && x.UserId != userId)
            .ToListAsync();

        var otherUserIds = otherParticipants.Select(x => x.UserId).Distinct().ToList();
        var blockedPairs = await _context.UserBlocks
            .AsNoTracking()
            .Where(x => (x.BlockerId == userId && otherUserIds.Contains(x.BlockedId)) || (x.BlockedId == userId && otherUserIds.Contains(x.BlockerId)))
            .ToListAsync();
        var blockedSet = blockedPairs.Select(x => x.BlockerId == userId ? x.BlockedId : x.BlockerId).ToHashSet();

        var followingIds = await _context.UserFollows
            .AsNoTracking()
            .Where(x => x.FollowerId == userId && otherUserIds.Contains(x.FollowingId))
            .Select(x => x.FollowingId)
            .ToListAsync();

        var followerIds = await _context.UserFollows
            .AsNoTracking()
            .Where(x => x.FollowingId == userId && otherUserIds.Contains(x.FollowerId))
            .Select(x => x.FollowerId)
            .ToListAsync();

        var followingSet = followingIds.ToHashSet();
        var followerSet = followerIds.ToHashSet();

        var threads = await _context.ChatThreads.AsNoTracking().Where(x => threadIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id);

        var lastMessages = await _context.ChatMessages
            .AsNoTracking()
            .Where(x => threadIds.Contains(x.ThreadId))
            .OrderByDescending(x => x.SentAt)
            .ToListAsync();

        var lastByThread = lastMessages
            .GroupBy(x => x.ThreadId)
            .ToDictionary(x => x.Key, x => x.First());

        var unreadByThread = await _context.ChatMessages
            .AsNoTracking()
            .Where(x => threadIds.Contains(x.ThreadId) && x.SenderId != userId && !x.IsDeleted && x.SeenAt == null)
            .GroupBy(x => x.ThreadId)
            .Select(g => new { ThreadId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ThreadId, x => x.Count);

        var results = otherParticipants
            .Where(participant => !blockedSet.Contains(participant.UserId))
            .Where(participant => lastByThread.ContainsKey(participant.ThreadId))
            .Where(participant => threads.ContainsKey(participant.ThreadId))
            .Select(participant =>
            {
                var thread = threads[participant.ThreadId];
                var rel = (isFollowing: followingSet.Contains(participant.UserId), followsMe: followerSet.Contains(participant.UserId));
                var isFriend = rel.isFollowing && rel.followsMe;
                var accepted = string.Equals(thread.RequestStatus, RequestAccepted, StringComparison.OrdinalIgnoreCase);
                var pending = string.Equals(thread.RequestStatus, RequestPending, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(thread.RequestStatus);
                var last = lastByThread[participant.ThreadId];

                var section = isFriend ? "friends" : accepted ? "chats" : pending && thread.RequestedByUserId == userId ? "sentRequests" : "requests";
                return new ThreadItem
                {
                    ThreadId = participant.ThreadId,
                    OtherUser = ToFrontendUser(participant.User, rel.isFollowing, rel.followsMe, blockedByMe: false, hasBlockedMe: false),
                    LastMessage = ToFrontendMessage(last, userId),
                    IsFriend = isFriend,
                    Section = section,
                    LastSentAt = last.SentAt,
                    RequestStatus = string.IsNullOrWhiteSpace(thread.RequestStatus) ? RequestPending : thread.RequestStatus,
                    RequestedByUserId = thread.RequestedByUserId,
                    IsAccepted = accepted,
                    UnreadCount = unreadByThread.GetValueOrDefault(participant.ThreadId)
                };
            })
            .Where(x => !string.Equals(x.RequestStatus, RequestDeleted, StringComparison.OrdinalIgnoreCase) && !string.Equals(x.RequestStatus, RequestBlocked, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.LastSentAt)
            .Select(x => new
            {
                threadId = x.ThreadId,
                otherUser = x.OtherUser,
                lastMessage = x.LastMessage,
                isFriend = x.IsFriend,
                section = x.Section,
                requestStatus = x.RequestStatus,
                requestedByUserId = x.RequestedByUserId,
                isAccepted = x.IsAccepted,
                unreadCount = x.UnreadCount
            })
            .ToList<object>();

        return results;
    }

    public async Task<List<object>> GetChatUsersAsync(Guid currentUserId)
    {
        var blockedIds = await GetBlockedEitherDirectionIdsAsync(currentUserId);
        var users = await _context.Users
            .AsNoTracking()
            .Where(x => x.Id != currentUserId && !blockedIds.Contains(x.Id))
            .OrderBy(x => x.Name)
            .Take(100)
            .ToListAsync();

        var ids = users.Select(x => x.Id).ToList();

        var followingIds = await _context.UserFollows
            .AsNoTracking()
            .Where(x => x.FollowerId == currentUserId && ids.Contains(x.FollowingId))
            .Select(x => x.FollowingId)
            .ToListAsync();

        var followerIds = await _context.UserFollows
            .AsNoTracking()
            .Where(x => x.FollowingId == currentUserId && ids.Contains(x.FollowerId))
            .Select(x => x.FollowerId)
            .ToListAsync();

        var followingSet = followingIds.ToHashSet();
        var followerSet = followerIds.ToHashSet();

        return users
            .Select(user => ToFrontendUser(user, followingSet.Contains(user.Id), followerSet.Contains(user.Id), false, false))
            .ToList<object>();
    }

    public async Task<object> AcceptMessageRequestAsync(Guid threadId, Guid userId)
    {
        var thread = await GetThreadForRequestActionAsync(threadId, userId);
        if (thread.RequestedByUserId == userId)
            throw new ApiException("You cannot accept a request you sent.", 400, "VALIDATION_ERROR");

        var otherId = await GetOtherParticipantIdAsync(threadId, userId);
        if (await IsBlockedPairAsync(userId, otherId))
            throw new ApiException("You cannot accept this request because one of you blocked the other.", 403, "USER_BLOCKED");

        thread.RequestStatus = RequestAccepted;
        thread.AcceptedAt = DateTime.UtcNow;
        thread.DeletedAt = null;

        var actor = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
        var actorName = DisplayName(actor);
        await AddNotificationAsync(otherId, $"{actorName} accepted your request", $"{actorName} accepted your message request.", "message_request_accepted", threadId, userId, $"/communityProfile?userId={userId}");
        await TouchPresenceAsync(userId);
        await _context.SaveChangesAsync();
        return new { threadId, requestStatus = thread.RequestStatus, acceptedAt = thread.AcceptedAt };
    }

    public async Task<object> DeleteMessageRequestAsync(Guid threadId, Guid userId)
    {
        var thread = await GetThreadForRequestActionAsync(threadId, userId);

        // Incoming request: receiver deletes it.
        // Sent request: sender cancels it before acceptance.
        // Both cases hide the pending conversation and allow a fresh request later.
        thread.RequestStatus = RequestDeleted;
        thread.DeletedAt = DateTime.UtcNow;
        await TouchPresenceAsync(userId);
        await _context.SaveChangesAsync();
        return new { threadId, requestStatus = thread.RequestStatus, deletedAt = thread.DeletedAt, cancelledByUserId = userId };
    }

    public async Task<object> BlockThreadUserAsync(Guid threadId, Guid userId)
    {
        var thread = await GetThreadForRequestActionAsync(threadId, userId, allowAccepted: true);
        var otherId = await GetOtherParticipantIdAsync(threadId, userId);

        if (!await _context.UserBlocks.AnyAsync(x => x.BlockerId == userId && x.BlockedId == otherId))
        {
            _context.UserBlocks.Add(new UserBlock { Id = Guid.NewGuid(), BlockerId = userId, BlockedId = otherId, CreatedAt = DateTime.UtcNow });
        }

        await _context.UserFollows.Where(x => (x.FollowerId == userId && x.FollowingId == otherId) || (x.FollowerId == otherId && x.FollowingId == userId)).ExecuteDeleteAsync();
        thread.RequestStatus = RequestBlocked;
        thread.DeletedAt = DateTime.UtcNow;
        await TouchPresenceAsync(userId);
        await _context.SaveChangesAsync();

        return new { threadId, blockedUserId = otherId, requestStatus = thread.RequestStatus };
    }

    private async Task<ChatThread> GetThreadForRequestActionAsync(Guid threadId, Guid userId, bool allowAccepted = false)
    {
        if (!await IsUserInThreadAsync(threadId, userId))
            throw new ApiException("Forbidden", 403, "FORBIDDEN");

        var thread = await _context.ChatThreads.FirstOrDefaultAsync(x => x.Id == threadId)
            ?? throw new ApiException("Thread not found", 404, "NOT_FOUND");

        if (!allowAccepted && !string.Equals(thread.RequestStatus, RequestPending, StringComparison.OrdinalIgnoreCase))
            throw new ApiException("This request is no longer pending.", 400, "REQUEST_NOT_PENDING");

        return thread;
    }

    private async Task<Guid> GetOtherParticipantIdAsync(Guid threadId, Guid userId)
    {
        var otherId = await _context.ChatParticipants
            .Where(x => x.ThreadId == threadId && x.UserId != userId)
            .Select(x => x.UserId)
            .FirstOrDefaultAsync();

        if (otherId == Guid.Empty)
            throw new ApiException("Conversation user not found", 404, "USER_NOT_FOUND");

        return otherId;
    }

    private async Task<bool> AllowsMessageRequestsAsync(Guid userId)
    {
        return await _context.Users.AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => x.AllowMessageRequests)
            .FirstOrDefaultAsync();
    }

    private async Task<bool> AreFriendsAsync(Guid firstUserId, Guid secondUserId)
    {
        var firstFollowsSecond = await _context.UserFollows.AnyAsync(x => x.FollowerId == firstUserId && x.FollowingId == secondUserId);
        var secondFollowsFirst = await _context.UserFollows.AnyAsync(x => x.FollowerId == secondUserId && x.FollowingId == firstUserId);
        return firstFollowsSecond && secondFollowsFirst;
    }

    private async Task<bool> IsBlockedPairAsync(Guid firstUserId, Guid secondUserId)
    {
        return await _context.UserBlocks.AnyAsync(x =>
            (x.BlockerId == firstUserId && x.BlockedId == secondUserId) ||
            (x.BlockerId == secondUserId && x.BlockedId == firstUserId));
    }

    private async Task<HashSet<Guid>> GetBlockedEitherDirectionIdsAsync(Guid userId)
    {
        var rows = await _context.UserBlocks
            .AsNoTracking()
            .Where(x => x.BlockerId == userId || x.BlockedId == userId)
            .Select(x => new { x.BlockerId, x.BlockedId })
            .ToListAsync();

        return rows.Select(x => x.BlockerId == userId ? x.BlockedId : x.BlockerId).ToHashSet();
    }

    private async Task AddNotificationAsync(Guid userId, string title, string message, string type, Guid relatedId, Guid? actorUserId = null, string? actionUrl = null)
    {
        var userPrefs = await _context.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new { x.NotificationsEnabled, x.MessageNotificationsEnabled })
            .FirstOrDefaultAsync();

        if (userPrefs == null || !userPrefs.NotificationsEnabled || !userPrefs.MessageNotificationsEnabled)
            return;

        _context.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ActorUserId = actorUserId,
            Title = title,
            Message = message,
            Type = type,
            RelatedEntityType = "ChatThread",
            RelatedEntityId = relatedId,
            ActionUrl = actionUrl,
            CreatedAt = DateTime.UtcNow
        });
    }

    private async Task TouchPresenceAsync(Guid userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user != null)
        {
            user.LastSeenAt = DateTime.UtcNow;
        }
    }

    private static string NormalizeMessageType(string? value)
    {
        var type = (value ?? "text").Trim().ToLowerInvariant();
        return type switch
        {
            "image" => "image",
            "video" => "video",
            "file" => "file",
            "audio" => "audio",
            "voice" => "audio",
            "post" => "post",
            _ => "text"
        };
    }

    private static DateTime AsUtc(DateTime value) => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    private static DateTime? AsUtc(DateTime? value) => value.HasValue ? AsUtc(value.Value) : null;
    private static string DisplayName(User? user) => user == null ? "Someone" : string.IsNullOrWhiteSpace(user.Username) ? user.Name : user.Username;
    private static string PreviewMessage(string senderName, string type, string text)
    {
        if (type != "text") return $"{senderName} sent a {type} message.";
        var preview = string.IsNullOrWhiteSpace(text) ? "a message" : text.Trim();
        if (preview.Length > 80) preview = $"{preview[..80]}...";
        return $"{senderName}: {preview}";
    }

    private static object ToFrontendMessage(ChatMessage msg, Guid currentUserId)
    {
        var isDeleted = msg.IsDeleted;
        var isMine = msg.SenderId == currentUserId;
        var text = isDeleted ? "This message was deleted" : msg.MessageText;
        var type = isDeleted ? "text" : msg.MessageType;
        var status = msg.SeenAt.HasValue ? "seen" : msg.DeliveredAt.HasValue ? "delivered" : "sent";

        return new
        {
            id = msg.Id,
            threadId = msg.ThreadId,
            senderId = msg.SenderId,
            messageText = text,
            text,
            type,
            messageType = type,
            mediaContent = isDeleted ? null : msg.MediaContent,
            content = isDeleted ? null : BuildFrontendContent(msg),
            fileName = isDeleted ? null : msg.FileName,
            sentAt = AsUtc(msg.SentAt),
            editedAt = AsUtc(msg.EditedAt),
            deletedAt = AsUtc(msg.DeletedAt),
            deliveredAt = AsUtc(msg.DeliveredAt),
            seenAt = AsUtc(msg.SeenAt),
            isEdited = msg.EditedAt.HasValue,
            isDeleted,
            canEdit = isMine && !isDeleted && msg.MessageType == "text",
            canDelete = isMine && !isDeleted,
            time = msg.SentAt.ToLocalTime().ToString("h:mm tt"),
            sender = isMine ? "me" : "them",
            status,
            unread = !isMine && !msg.SeenAt.HasValue,
            liked = false
        };
    }

    private static object? BuildFrontendContent(ChatMessage msg)
    {
        if (msg.MessageType == "file")
            return new { name = string.IsNullOrWhiteSpace(msg.FileName) ? "File" : msg.FileName, size = "saved", url = msg.MediaContent };

        if (msg.MessageType == "post" && !string.IsNullOrWhiteSpace(msg.MediaContent))
        {
            try { return JsonSerializer.Deserialize<object>(msg.MediaContent); }
            catch { return msg.MediaContent; }
        }

        return msg.MediaContent;
    }

    private static bool IsOnline(User user)
    {
        return user.LastSeenAt.HasValue && DateTime.UtcNow - user.LastSeenAt.Value <= TimeSpan.FromMinutes(2);
    }

    private static string FormatLastSeen(DateTime? lastSeenAt)
    {
        if (!lastSeenAt.HasValue) return "not active yet";
        var diff = DateTime.UtcNow - lastSeenAt.Value;
        if (diff.TotalMinutes < 1) return "just now";
        if (diff.TotalMinutes < 60) return $"{Math.Floor(diff.TotalMinutes)} min ago";
        if (diff.TotalHours < 24) return $"{Math.Floor(diff.TotalHours)} hours ago";
        return $"{Math.Floor(diff.TotalDays)} days ago";
    }

    private static object ToFrontendUser(User user, bool isFollowing, bool followsMe, bool blockedByMe, bool hasBlockedMe) => new
    {
        id = user.Id,
        name = !string.IsNullOrWhiteSpace(user.Username) ? user.Username : user.Name,
        fullName = user.Name,
        username = user.Username,
        email = user.Email,
        avatar = string.IsNullOrWhiteSpace(user.ProfileImageUrl) ? null : user.ProfileImageUrl,
        profileImage = user.ProfileImageUrl,
        gender = user.Gender,
        online = user.ShowOnlineStatus && IsOnline(user),
        activeNow = user.ShowOnlineStatus && IsOnline(user),
        lastSeen = user.ShowLastSeen ? FormatLastSeen(user.LastSeenAt) : "hidden",
        lastSeenAt = user.ShowLastSeen ? AsUtc(user.LastSeenAt) : null,
        isFollowing,
        followsMe,
        isFriend = isFollowing && followsMe,
        blockedByMe,
        hasBlockedMe
    };

    private class ThreadItem
    {
        public Guid ThreadId { get; set; }
        public object? OtherUser { get; set; }
        public object? LastMessage { get; set; }
        public bool IsFriend { get; set; }
        public string Section { get; set; } = "friends";
        public DateTime LastSentAt { get; set; }
        public string RequestStatus { get; set; } = RequestPending;
        public Guid? RequestedByUserId { get; set; }
        public bool IsAccepted { get; set; }
        public int UnreadCount { get; set; }
    }
}

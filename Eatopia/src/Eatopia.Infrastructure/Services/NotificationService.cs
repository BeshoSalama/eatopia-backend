using Eatopia.Application.DTOs.Notifications;
using Eatopia.Application.Exceptions;
using Eatopia.Domain.Entities;
using Eatopia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eatopia.Infrastructure.Services;

public class NotificationService
{
    private readonly EatopiaDbContext _context;
    private readonly EmailService _emailService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(EatopiaDbContext context, EmailService emailService, ILogger<NotificationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<NotificationResponseDto> CreateAsync(Guid userId, CreateNotificationDto dto)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null)
            throw new ApiException("User not found", 404, "USER_NOT_FOUND");

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = dto.Title.Trim(),
            Message = dto.Message.Trim(),
            Type = string.IsNullOrWhiteSpace(dto.Type) ? "info" : dto.Type.Trim(),
            ScheduledFor = dto.ScheduledFor,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);

        if (dto.SendEmail && user.EmailNotificationsEnabled)
        {
            var sent = await _emailService.SendAsync(user.Email, notification.Title, notification.Message);
            notification.EmailSent = sent;
            notification.EmailSentAt = sent ? DateTime.UtcNow : null;
        }

        await _context.SaveChangesAsync();
        return ToDto(notification);
    }

    public async Task<List<NotificationResponseDto>> GetUserNotificationsAsync(Guid userId, bool unreadOnly = false)
    {
        var enabled = await _context.Users.AsNoTracking().Where(x => x.Id == userId).Select(x => x.NotificationsEnabled).FirstOrDefaultAsync();
        if (!enabled) return new List<NotificationResponseDto>();

        var query = _context.Notifications.AsNoTracking().Include(x => x.ActorUser).Where(x => x.UserId == userId);
        if (unreadOnly)
            query = query.Where(x => !x.IsRead);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(50)
            .ToListAsync();

        return items.Select(ToDto).ToList();
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        var enabled = await _context.Users.AsNoTracking().Where(x => x.Id == userId).Select(x => x.NotificationsEnabled).FirstOrDefaultAsync();
        if (!enabled) return 0;
        return await _context.Notifications.CountAsync(x => x.UserId == userId && !x.IsRead);
    }

    public async Task MarkAsReadAsync(Guid userId, Guid notificationId)
    {
        var item = await _context.Notifications.FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId);
        if (item == null)
            throw new ApiException("Notification not found", 404, "NOT_FOUND");

        item.IsRead = true;
        item.ReadAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        var items = await _context.Notifications.Where(x => x.UserId == userId && !x.IsRead).ToListAsync();
        foreach (var item in items)
        {
            item.IsRead = true;
            item.ReadAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();
    }

    public async Task<bool> TrySendNotificationEmailAsync(Guid notificationId)
    {
        var notification = await _context.Notifications.FirstOrDefaultAsync(x => x.Id == notificationId);
        if (notification == null) return false;
        if (notification.EmailSent) return true;

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == notification.UserId);
        if (user == null || string.IsNullOrWhiteSpace(user.Email) || !user.NotificationsEnabled || !user.EmailNotificationsEnabled) return false;

        var sent = await _emailService.SendAsync(user.Email, notification.Title, notification.Message);
        notification.EmailSent = sent;
        notification.EmailSentAt = sent ? DateTime.UtcNow : notification.EmailSentAt;
        await _context.SaveChangesAsync();
        return sent;
    }

    public async Task CreateSystemNotificationAsync(Guid userId, string title, string message, string type = "info", bool sendEmail = false, string? relatedType = null, Guid? relatedId = null, DateTime? scheduledFor = null)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null || !user.NotificationsEnabled)
            return;

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            RelatedEntityType = relatedType,
            RelatedEntityId = relatedId,
            ScheduledFor = scheduledFor,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);

        if (sendEmail && user.EmailNotificationsEnabled)
        {
            var sent = await _emailService.SendAsync(user.Email, title, message);
            notification.EmailSent = sent;
            notification.EmailSentAt = sent ? DateTime.UtcNow : null;

            if (!sent)
            {
                _logger.LogWarning("Notification created but email was not sent to {Email}. It will be retried by the reminder worker if the same reminder is still due.", user.Email);
            }
        }
    }

    private static NotificationResponseDto ToDto(Notification n) => new()
    {
        Id = n.Id,
        Title = n.Title,
        Message = n.Message,
        Type = n.Type,
        ActorUserId = n.ActorUserId,
        Actor = n.ActorUser == null ? null : new NotificationActorDto
        {
            Id = n.ActorUser.Id,
            Name = string.IsNullOrWhiteSpace(n.ActorUser.Username) ? n.ActorUser.Name : n.ActorUser.Username,
            FullName = n.ActorUser.Name,
            Username = n.ActorUser.Username,
            Avatar = n.ActorUser.ProfileImageUrl,
            ProfileImage = n.ActorUser.ProfileImageUrl,
            Gender = n.ActorUser.Gender
        },
        RelatedEntityType = n.RelatedEntityType,
        RelatedEntityId = n.RelatedEntityId,
        ActionUrl = n.ActionUrl,
        IsRead = n.IsRead,
        CreatedAt = n.CreatedAt,
        ReadAt = n.ReadAt,
        ScheduledFor = n.ScheduledFor,
        EmailSent = n.EmailSent
    };
}

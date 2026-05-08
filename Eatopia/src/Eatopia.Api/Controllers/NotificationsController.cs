using Eatopia.Application.DTOs.Notifications;
using Eatopia.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eatopia.Api.Controllers;

[Route("api/notifications")]
[ApiController]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly NotificationService _notificationService;

    public NotificationsController(NotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool unreadOnly = false)
    {
        var notifications = await _notificationService.GetUserNotificationsAsync(GetUserId(), unreadOnly);
        var unreadCount = await _notificationService.GetUnreadCountAsync(GetUserId());
        return Ok(new { success = true, notifications, unreadCount, data = notifications });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateNotificationDto dto)
    {
        var notification = await _notificationService.CreateAsync(GetUserId(), dto);
        return Ok(new { success = true, message = "Notification sent.", notification, data = notification });
    }

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        await _notificationService.MarkAsReadAsync(GetUserId(), id);
        return Ok(new { success = true });
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await _notificationService.MarkAllAsReadAsync(GetUserId());
        return Ok(new { success = true });
    }

    [HttpPost("test-email")]
    public async Task<IActionResult> SendTestEmail()
    {
        var notification = await _notificationService.CreateAsync(GetUserId(), new CreateNotificationDto
        {
            Title = "Eatopia test notification",
            Message = "If you received this email, Gmail SMTP settings are working correctly.",
            Type = "test",
            SendEmail = true,
            ScheduledFor = DateTime.UtcNow
        });

        return Ok(new { success = true, message = notification.EmailSent ? "Test email sent." : "Notification created, but email was not sent. Check API logs and Gmail App Password.", notification });
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

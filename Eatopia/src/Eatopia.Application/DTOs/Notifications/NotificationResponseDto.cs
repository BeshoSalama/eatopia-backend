namespace Eatopia.Application.DTOs.Notifications;

public class NotificationResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string Type { get; set; } = "info";
    public Guid? ActorUserId { get; set; }
    public NotificationActorDto? Actor { get; set; }
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public bool EmailSent { get; set; }
}

public class NotificationActorDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "User";
    public string FullName { get; set; } = "User";
    public string? Username { get; set; }
    public string? Avatar { get; set; }
    public string? ProfileImage { get; set; }
    public string? Gender { get; set; }
}

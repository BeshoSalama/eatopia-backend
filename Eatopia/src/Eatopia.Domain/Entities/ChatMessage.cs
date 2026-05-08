using Eatopia.Domain.Common;

namespace Eatopia.Domain.Entities;

public class ChatMessage : BaseEntity
{
    public Guid ThreadId { get; set; }
    public ChatThread Thread { get; set; } = null!;

    public Guid SenderId { get; set; }
    public User Sender { get; set; } = null!;

    public string MessageText { get; set; } = string.Empty;
    public string MessageType { get; set; } = "text";
    public string? MediaContent { get; set; }
    public string? FileName { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; } = false;
    public DateTime? EditedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? SeenAt { get; set; }
}

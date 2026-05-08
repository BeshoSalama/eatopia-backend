using Eatopia.Domain.Common;

namespace Eatopia.Domain.Entities;

public class ChatParticipant : BaseEntity
{
    public Guid ThreadId { get; set; }
    public ChatThread Thread { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

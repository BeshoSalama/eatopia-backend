using Eatopia.Domain.Common;

namespace Eatopia.Domain.Entities;

public class ChatThread : BaseEntity
{
    public string RequestStatus { get; set; } = "Pending";
    public Guid? RequestedByUserId { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<ChatParticipant> Participants { get; set; } = new List<ChatParticipant>();
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}

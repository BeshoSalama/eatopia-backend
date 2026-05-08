using Eatopia.Domain.Common;

namespace Eatopia.Domain.Entities;

public class Comment : BaseEntity
{
    public Guid PostId { get; set; }
    public CommunityPost Post { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Text { get; set; } = null!;
}

using Eatopia.Domain.Common;

namespace Eatopia.Domain.Entities;

public class HiddenPost : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid PostId { get; set; }
    public CommunityPost Post { get; set; } = null!;
}

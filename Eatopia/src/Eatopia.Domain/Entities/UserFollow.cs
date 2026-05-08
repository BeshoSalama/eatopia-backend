using Eatopia.Domain.Common;

namespace Eatopia.Domain.Entities;

public class UserFollow : BaseEntity
{
    public Guid FollowerId { get; set; }
    public User Follower { get; set; } = null!;

    public Guid FollowingId { get; set; }
    public User Following { get; set; } = null!;
}

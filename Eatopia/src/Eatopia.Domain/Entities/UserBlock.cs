using Eatopia.Domain.Common;

namespace Eatopia.Domain.Entities;

public class UserBlock : BaseEntity
{
    public Guid BlockerId { get; set; }
    public User Blocker { get; set; } = null!;

    public Guid BlockedId { get; set; }
    public User Blocked { get; set; } = null!;
}

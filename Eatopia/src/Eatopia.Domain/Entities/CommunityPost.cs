using Eatopia.Domain.Common;

namespace Eatopia.Domain.Entities;

public class CommunityPost : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }

    public Guid? SharedPostId { get; set; }
    public CommunityPost? SharedPost { get; set; }

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}

using Eatopia.Domain.Common;

namespace Eatopia.Domain.Entities;

public class PasswordResetCode : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string CodeHash { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public bool IsUsed { get; set; } = false;
    public int AttemptCount { get; set; } = 0;
    public DateTime? LastAttemptAt { get; set; }
}

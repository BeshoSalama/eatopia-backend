using Eatopia.Domain.Common;

namespace Eatopia.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string? Username { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Location { get; set; }
    public string? Phone { get; set; }
    public string? ProfileImageUrl { get; set; }

    public int? Age { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? HeightCm { get; set; }

    public string? Gender { get; set; }
    public string? ActivityLevel { get; set; }
    public string? Goal { get; set; }

    public string Role { get; set; } = "User";
    public bool IsBanned { get; set; } = false;
    public DateTime? BannedAt { get; set; }
    public string? BannedReason { get; set; }

    public string AuthProvider { get; set; } = "Local";
    public string? ExternalProviderId { get; set; }
    public bool EmailConfirmed { get; set; } = false;
    public DateTime? EmailConfirmedAt { get; set; }
    public string? EmailConfirmationTokenHash { get; set; }
    public DateTime? EmailConfirmationTokenExpiresAt { get; set; }
    public DateTime? LastEmailConfirmationSentAt { get; set; }

    public DateTime? LastSeenAt { get; set; }

    public int FailedLoginAttemptCount { get; set; } = 0;
    public DateTime? LoginLockoutEndAt { get; set; }
    public int JwtTokenVersion { get; set; } = 0;

    // Privacy + notification preferences
    public bool NotificationsEnabled { get; set; } = true;
    public bool MessageNotificationsEnabled { get; set; } = true;
    public bool CommunityNotificationsEnabled { get; set; } = true;
    public bool EmailNotificationsEnabled { get; set; } = true;
    public string ProfileVisibility { get; set; } = "Public";
    public string PostsVisibility { get; set; } = "Public";
    public bool ShowOnlineStatus { get; set; } = true;
    public bool ShowLastSeen { get; set; } = true;
    public bool AllowMessageRequests { get; set; } = true;
    public bool AllowSearchByEmail { get; set; } = true;

    public ICollection<UserAllergy> Allergies { get; set; } = new List<UserAllergy>();
    public ICollection<UserDislikedFood> DislikedFoods { get; set; } = new List<UserDislikedFood>();
}

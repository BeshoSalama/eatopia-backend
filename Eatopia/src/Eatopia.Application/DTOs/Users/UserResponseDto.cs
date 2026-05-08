using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.Users;

public class UserResponseDto
{
    public Guid Id { get; set; }

    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = null!;

    public string Name
    {
        get => FullName;
        set => FullName = value;
    }

    public string? Username { get; set; }
    public string Email { get; set; } = null!;

    [JsonPropertyName("emailConfirmed")]
    public bool EmailConfirmed { get; set; }

    public string? Gender { get; set; }

    [JsonPropertyName("birthDate")]
    public DateTime? BirthDate { get; set; }

    public string? Location { get; set; }
    public string? Phone { get; set; }

    [JsonPropertyName("profileImage")]
    public string? ProfileImage { get; set; }

    public string? Avatar
    {
        get => ProfileImage;
        set => ProfileImage = value;
    }

    public int? Age { get; set; }

    [JsonPropertyName("weight")]
    public decimal? WeightKg { get; set; }

    [JsonPropertyName("height")]
    public decimal? HeightCm { get; set; }

    public string? Goal { get; set; }

    [JsonPropertyName("activityLevel")]
    public string? ActivityLevel { get; set; }

    public string Role { get; set; } = "User";

    [JsonPropertyName("notificationsEnabled")]
    public bool NotificationsEnabled { get; set; } = true;

    [JsonPropertyName("messageNotificationsEnabled")]
    public bool MessageNotificationsEnabled { get; set; } = true;

    [JsonPropertyName("communityNotificationsEnabled")]
    public bool CommunityNotificationsEnabled { get; set; } = true;

    [JsonPropertyName("emailNotificationsEnabled")]
    public bool EmailNotificationsEnabled { get; set; } = true;

    [JsonPropertyName("profileVisibility")]
    public string ProfileVisibility { get; set; } = "Public";

    [JsonPropertyName("postsVisibility")]
    public string PostsVisibility { get; set; } = "Public";

    [JsonPropertyName("showOnlineStatus")]
    public bool ShowOnlineStatus { get; set; } = true;

    [JsonPropertyName("showLastSeen")]
    public bool ShowLastSeen { get; set; } = true;

    [JsonPropertyName("allowMessageRequests")]
    public bool AllowMessageRequests { get; set; } = true;

    [JsonPropertyName("allowSearchByEmail")]
    public bool AllowSearchByEmail { get; set; } = true;
}

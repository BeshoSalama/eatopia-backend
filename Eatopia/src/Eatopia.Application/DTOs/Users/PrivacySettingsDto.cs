using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.Users;

public class PrivacySettingsDto
{
    [JsonPropertyName("notificationsEnabled")]
    public bool NotificationsEnabled { get; set; } = true;

    [JsonPropertyName("messageNotificationsEnabled")]
    public bool MessageNotificationsEnabled { get; set; } = true;

    [JsonPropertyName("communityNotificationsEnabled")]
    public bool CommunityNotificationsEnabled { get; set; } = true;

    [JsonPropertyName("emailNotificationsEnabled")]
    public bool EmailNotificationsEnabled { get; set; } = true;

    [JsonPropertyName("profileVisibility")]
    [MaxLength(20)]
    public string ProfileVisibility { get; set; } = "Public";

    [JsonPropertyName("postsVisibility")]
    [MaxLength(20)]
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

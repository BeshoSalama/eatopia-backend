namespace Eatopia.Domain.Auth;

public static class UserRoles
{
    public const string Owner = "Owner";
    public const string Admin = "Admin";
    public const string User = "User";
    public const string Elevated = Owner + "," + Admin;

    public static string? Normalize(string? role)
    {
        var value = role?.Trim().ToLowerInvariant();

        return value switch
        {
            "owner" or "super admin" or "superadmin" => Owner,
            "admin" or "manager" => Admin,
            "user" => User,
            _ => null
        };
    }
}

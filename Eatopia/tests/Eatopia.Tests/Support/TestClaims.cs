using System.Security.Claims;
using Eatopia.Domain.Auth;

namespace Eatopia.Tests.Support;

public static class TestClaims
{
    public static ClaimsPrincipal Principal(Guid userId, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, "Test User"),
            new(ClaimTypes.Email, "test@eatopia.local")
        };

        foreach (var role in roles.DefaultIfEmpty(UserRoles.User))
            claims.Add(new Claim(ClaimTypes.Role, role));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }
}

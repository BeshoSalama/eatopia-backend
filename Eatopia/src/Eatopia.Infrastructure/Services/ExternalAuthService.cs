using Eatopia.Application.DTOs.Auth;
using Eatopia.Application.DTOs.SocialAuth;
using Eatopia.Application.Exceptions;
using Eatopia.Domain.Entities;
using Eatopia.Infrastructure.Persistence;
using Eatopia.Infrastructure.Security;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace Eatopia.Infrastructure.Services;

public class ExternalAuthService
{
    private readonly EatopiaDbContext _context;
    private readonly JwtService _jwtService;
    private readonly IConfiguration _configuration;

    public ExternalAuthService(EatopiaDbContext context, JwtService jwtService, IConfiguration configuration)
    {
        _context = context;
        _jwtService = jwtService;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> LoginAsync(SocialLoginRequestDto dto)
    {
        var provider = dto.Provider.Trim().ToLowerInvariant();

        ExternalUser externalUser = provider switch
        {
            "google" => await VerifyGoogleAsync(dto.IdToken),
            _ => throw new ApiException("Only Google social login is currently supported.", 400, "UNSUPPORTED_PROVIDER")
        };

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == externalUser.Email);

        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = externalUser.Email,
                Username = await BuildUniqueUsernameAsync(externalUser.Email),
                Name = !string.IsNullOrWhiteSpace(dto.FullName) ? dto.FullName.Trim() : externalUser.FullName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N") + "!Aa1"),
                ProfileImageUrl = externalUser.Picture,
                AuthProvider = provider,
                ExternalProviderId = externalUser.Subject,
                EmailConfirmed = true,
                Role = "User",
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
        }
        else
        {
            user.AuthProvider = provider;
            user.ExternalProviderId = externalUser.Subject;
            user.EmailConfirmed = true;
            if (string.IsNullOrWhiteSpace(user.ProfileImageUrl) && !string.IsNullOrWhiteSpace(externalUser.Picture))
                user.ProfileImageUrl = externalUser.Picture;
            if (string.IsNullOrWhiteSpace(user.Name) && !string.IsNullOrWhiteSpace(externalUser.FullName))
                user.Name = externalUser.FullName;
        }

        var now = DateTime.UtcNow;
        user.LastSeenAt = now;
        var refresh = CreateRefreshToken(user.Id, now);
        _context.RefreshTokens.Add(refresh.Entity);
        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            Token = _jwtService.GenerateToken(user),
            RefreshToken = refresh.PlainToken,
            RefreshTokenExpiresAt = refresh.Entity.ExpiresAt,
            User = AuthService.ToUserResponse(user)
        };
    }

    private async Task<ExternalUser> VerifyGoogleAsync(string idToken)
    {
        var clientId = _configuration["Authentication:Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId) || clientId.Contains("PUT_GOOGLE"))
            throw new ApiException("Google ClientId is missing in appsettings.", 500, "GOOGLE_CLIENT_ID_MISSING");

        var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { clientId }
        });

        if (string.IsNullOrWhiteSpace(payload.Email))
            throw new ApiException("Google account did not return an email.", 400, "EMAIL_MISSING");

        return new ExternalUser(payload.Subject, payload.Email.ToLowerInvariant(), payload.Name ?? payload.Email, payload.Picture);
    }

    private async Task<string> BuildUniqueUsernameAsync(string email)
    {
        var baseName = email.Split('@')[0].Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(baseName)) baseName = "user";
        var candidate = baseName;
        var suffix = 1;
        while (await _context.Users.AnyAsync(x => x.Username == candidate))
        {
            candidate = $"{baseName}{suffix++}";
        }
        return candidate;
    }

    private static (RefreshToken Entity, string PlainToken) CreateRefreshToken(Guid userId, DateTime now)
    {
        var plain = GenerateUrlSafeToken();
        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(plain),
            ExpiresAt = now.AddDays(14),
            CreatedAt = now
        };

        return (entity, plain);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    private static string GenerateUrlSafeToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", string.Empty);
    }

    private sealed record ExternalUser(string Subject, string Email, string FullName, string? Picture);
}

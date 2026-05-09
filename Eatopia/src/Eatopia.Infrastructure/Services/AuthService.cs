using Eatopia.Application.DTOs.Auth;
using Eatopia.Application.DTOs.Users;
using Eatopia.Application.Exceptions;
using Eatopia.Domain.Entities;
using Eatopia.Infrastructure.Persistence;
using Eatopia.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace Eatopia.Infrastructure.Services;

public class AuthService
{
    private readonly EatopiaDbContext _context;
    private readonly JwtService _jwtService;
    private readonly EmailService _emailService;
    private readonly IConfiguration _configuration;

    public AuthService(EatopiaDbContext context, JwtService jwtService, EmailService emailService, IConfiguration configuration)
    {
        _context = context;
        _jwtService = jwtService;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto)
    {
        dto.Email = dto.Email.Trim().ToLowerInvariant();
        var name = (dto.FullName ?? dto.Name)?.Trim();
        if (string.IsNullOrWhiteSpace(name)) throw new ApiException("Full name is required", 400, "VALIDATION_ERROR");
        ValidatePasswordStrength(dto.Password);
        var username = string.IsNullOrWhiteSpace(dto.Username) ? await BuildUniqueUsernameFromEmailAsync(dto.Email) : dto.Username.Trim();
        if (await _context.Users.AnyAsync(x => x.Email == dto.Email)) throw new ApiException("Email already exists", 409, "EMAIL_EXISTS");
        if (await _context.Users.AnyAsync(x => x.Username != null && x.Username.ToLower() == username.ToLower())) throw new ApiException("Username already exists", 409, "USERNAME_EXISTS");

        var now = DateTime.UtcNow;
        var activationToken = GenerateUrlSafeToken();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = dto.Email,
            Username = username,
            Name = name,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            BirthDate = dto.BirthDate.HasValue
                ? AsUtc(dto.BirthDate.Value)
                : null,
            Location = dto.Location?.Trim(),
            Gender = dto.Gender?.Trim(),
            Phone = dto.Phone?.Trim(),
            ProfileImageUrl = dto.ProfileImage?.Trim(),
            Role = "User",
            AuthProvider = "Local",
            EmailConfirmed = false,
            EmailConfirmationTokenHash = BCrypt.Net.BCrypt.HashPassword(activationToken),
            EmailConfirmationTokenExpiresAt = now.AddHours(24),
            LastEmailConfirmationSentAt = now,
            CreatedAt = now,
            LastSeenAt = null
        };

        if (dto.BirthDate.HasValue) user.Age = CalculateAge(dto.BirthDate.Value);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var sent = await SendActivationEmailAsync(user, activationToken);
        if (!sent)
        {
            if (CanBypassActivationEmailForDevelopment())
            {
                user.EmailConfirmed = true;
                user.EmailConfirmedAt = DateTime.UtcNow;
                user.EmailConfirmationTokenHash = null;
                user.EmailConfirmationTokenExpiresAt = null;
                await _context.SaveChangesAsync();
            }
            else
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                throw new ApiException("Activation email could not be sent. Check Email settings in appsettings.json before creating the account.", 500, "EMAIL_SEND_FAILED");
            }
        }

        return new AuthResponseDto { Token = string.Empty, User = ToUserResponse(user) };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
    {
        var identifier = (dto.UsernameOrEmail ?? dto.Email)?.Trim();
        if (string.IsNullOrWhiteSpace(identifier)) throw new ApiException("Username or email is required", 400, "VALIDATION_ERROR");
        var normalizedIdentifier = identifier.ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedIdentifier || (x.Username != null && x.Username.ToLower() == normalizedIdentifier));
        if (user == null) throw new ApiException("Invalid username/email or password", 401, "INVALID_CREDENTIALS");

        var now = DateTime.UtcNow;
        if (user.IsBanned)
            throw new ApiException(string.IsNullOrWhiteSpace(user.BannedReason) ? "This account is banned." : $"This account is banned: {user.BannedReason}", 403, "ACCOUNT_BANNED");

        if (user.LoginLockoutEndAt.HasValue && user.LoginLockoutEndAt.Value > now)
            throw new ApiException("Too many failed login attempts. Please try again later.", 429, "LOGIN_LOCKED");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            user.FailedLoginAttemptCount += 1;
            if (user.FailedLoginAttemptCount >= 5)
            {
                user.LoginLockoutEndAt = now.AddMinutes(15);
                user.FailedLoginAttemptCount = 0;
            }

            await _context.SaveChangesAsync();
            throw new ApiException("Invalid username/email or password", 401, "INVALID_CREDENTIALS");
        }

        if (user.AuthProvider == "Local" && !user.EmailConfirmed)
        {
            // Legacy local accounts created before activation links existed have no pending token.
            // Confirm them once to avoid locking current users out after updating the project.
            if (string.IsNullOrWhiteSpace(user.EmailConfirmationTokenHash))
            {
                user.EmailConfirmed = true;
                user.EmailConfirmedAt = now;
            }
            else
            {
                throw new ApiException("Please activate your account from the email link before logging in.", 403, "EMAIL_NOT_CONFIRMED");
            }
        }

        user.FailedLoginAttemptCount = 0;
        user.LoginLockoutEndAt = null;
        user.LastSeenAt = now;

        var refresh = CreateRefreshToken(user.Id, now);
        _context.RefreshTokens.Add(refresh.Entity);

        await _context.SaveChangesAsync();
        return new AuthResponseDto
        {
            Token = _jwtService.GenerateToken(user),
            RefreshToken = refresh.PlainToken,
            RefreshTokenExpiresAt = refresh.Entity.ExpiresAt,
            User = ToUserResponse(user)
        };
    }

    public async Task ActivateAccountAsync(ActivateAccountRequestDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var token = dto.Token.Trim();

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (user == null)
            throw new ApiException("Invalid or expired activation link.", 400, "INVALID_ACTIVATION_LINK");

        if (user.EmailConfirmed)
            return;

        if (string.IsNullOrWhiteSpace(user.EmailConfirmationTokenHash) ||
            !user.EmailConfirmationTokenExpiresAt.HasValue ||
            user.EmailConfirmationTokenExpiresAt.Value < DateTime.UtcNow)
            throw new ApiException("Invalid or expired activation link. Please request a new activation email.", 400, "INVALID_ACTIVATION_LINK");

        if (!BCrypt.Net.BCrypt.Verify(token, user.EmailConfirmationTokenHash))
            throw new ApiException("Invalid or expired activation link.", 400, "INVALID_ACTIVATION_LINK");

        user.EmailConfirmed = true;
        user.EmailConfirmedAt = DateTime.UtcNow;
        user.EmailConfirmationTokenHash = null;
        user.EmailConfirmationTokenExpiresAt = null;
        await _context.SaveChangesAsync();
    }

    public async Task ResendActivationEmailAsync(ResendActivationEmailRequestDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);

        // Keep the response generic for unknown/confirmed accounts to avoid user enumeration.
        if (user == null || user.EmailConfirmed)
            return;

        var now = DateTime.UtcNow;
        if (user.LastEmailConfirmationSentAt.HasValue && user.LastEmailConfirmationSentAt.Value > now.AddSeconds(-60))
            throw new ApiException("Please wait one minute before requesting another activation email.", 429, "ACTIVATION_EMAIL_COOLDOWN");

        var activationToken = GenerateUrlSafeToken();
        user.EmailConfirmationTokenHash = BCrypt.Net.BCrypt.HashPassword(activationToken);
        user.EmailConfirmationTokenExpiresAt = now.AddHours(24);
        user.LastEmailConfirmationSentAt = now;
        await _context.SaveChangesAsync();

        var sent = await SendActivationEmailAsync(user, activationToken);
        if (!sent)
        {
            if (CanBypassActivationEmailForDevelopment())
            {
                user.EmailConfirmed = true;
                user.EmailConfirmedAt = DateTime.UtcNow;
                user.EmailConfirmationTokenHash = null;
                user.EmailConfirmationTokenExpiresAt = null;
                await _context.SaveChangesAsync();
                return;
            }

            throw new ApiException("Activation email could not be sent. Check Email settings in appsettings.json.", 500, "EMAIL_SEND_FAILED");
        }
    }

    public async Task RequestPasswordResetAsync(ForgotPasswordRequestDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);

        // Keep the response generic for unknown emails to avoid user enumeration.
        if (user == null) return;

        var now = DateTime.UtcNow;
        var latestCode = await _context.PasswordResetCodes
            .Where(x => x.UserId == user.Id)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (latestCode != null && !latestCode.IsUsed && latestCode.CreatedAt > now.AddSeconds(-60))
            throw new ApiException("Please wait one minute before requesting another reset code.", 429, "RESET_CODE_COOLDOWN");

        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

        var oldCodes = await _context.PasswordResetCodes
            .Where(x => x.UserId == user.Id && !x.IsUsed)
            .ToListAsync();

        foreach (var old in oldCodes)
        {
            old.IsUsed = true;
            old.UsedAt = now;
        }

        _context.PasswordResetCodes.Add(new PasswordResetCode
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CodeHash = BCrypt.Net.BCrypt.HashPassword(code),
            ExpiresAt = now.AddMinutes(15),
            AttemptCount = 0,
            CreatedAt = now
        });

        await _context.SaveChangesAsync();

        var sent = await _emailService.SendAsync(
            user.Email,
            "Eatopia password reset code",
            $"Your Eatopia password reset code is: {code}\n\nThis code expires in 15 minutes. If you did not request it, ignore this email.");

        if (!sent)
            throw new ApiException("Reset code was created but email could not be sent. Check Email settings in appsettings.json.", 500, "EMAIL_SEND_FAILED");
    }

    public async Task ResetPasswordAsync(ResetPasswordRequestDto dto)
    {
        if (dto.NewPassword != dto.ConfirmPassword)
            throw new ApiException("Passwords do not match", 400, "PASSWORD_MISMATCH");
        ValidatePasswordStrength(dto.NewPassword);

        var email = dto.Email.Trim().ToLowerInvariant();
        var code = dto.Code.Trim();

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (user == null)
            throw new ApiException("Invalid or expired reset code", 400, "INVALID_RESET_CODE");

        var now = DateTime.UtcNow;
        var activeCodes = await _context.PasswordResetCodes
            .Where(x => x.UserId == user.Id && !x.IsUsed && x.ExpiresAt >= now)
            .OrderByDescending(x => x.CreatedAt)
            .Take(3)
            .ToListAsync();

        if (!activeCodes.Any())
            throw new ApiException("Invalid or expired reset code", 400, "INVALID_RESET_CODE");

        PasswordResetCode? match = null;

        foreach (var resetCode in activeCodes.Where(x => x.AttemptCount < 5))
        {
            if (BCrypt.Net.BCrypt.Verify(code, resetCode.CodeHash))
            {
                match = resetCode;
                break;
            }
        }

        if (match == null)
        {
            var latest = activeCodes.First();
            latest.AttemptCount += 1;
            latest.LastAttemptAt = now;

            if (latest.AttemptCount >= 5)
            {
                latest.IsUsed = true;
                latest.UsedAt = now;
            }

            await _context.SaveChangesAsync();
            throw new ApiException("Invalid or expired reset code", 400, "INVALID_RESET_CODE");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.LastSeenAt = now;

        foreach (var resetCode in activeCodes)
        {
            resetCode.IsUsed = true;
            resetCode.UsedAt = now;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto)
    {
        var token = dto.RefreshToken?.Trim();
        if (string.IsNullOrWhiteSpace(token))
            throw new ApiException("Refresh token is required", 400, "VALIDATION_ERROR");

        var tokenHash = HashToken(token);
        var stored = await _context.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == tokenHash);
        if (stored == null || stored.IsRevoked || stored.ExpiresAt <= DateTime.UtcNow)
            throw new ApiException("Invalid refresh token", 401, "INVALID_REFRESH_TOKEN");

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == stored.UserId)
            ?? throw new ApiException("User not found", 404, "USER_NOT_FOUND");

        var now = DateTime.UtcNow;
        var refresh = CreateRefreshToken(user.Id, now);
        stored.RevokedAt = now;
        stored.ReplacedByTokenHash = refresh.Entity.TokenHash;
        _context.RefreshTokens.Add(refresh.Entity);

        user.LastSeenAt = now;
        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            Token = _jwtService.GenerateToken(user),
            RefreshToken = refresh.PlainToken,
            RefreshTokenExpiresAt = refresh.Entity.ExpiresAt,
            User = ToUserResponse(user)
        };
    }

    public async Task LogoutAllDevicesAsync(Guid userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId)
            ?? throw new ApiException("User not found", 404, "USER_NOT_FOUND");

        user.JwtTokenVersion += 1;

        var now = DateTime.UtcNow;
        await _context.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, now));

        await _context.SaveChangesAsync();
    }

    public async Task<UserResponseDto> GetProfileAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId) ?? throw new ApiException("User not found", 404, "USER_NOT_FOUND");
        return ToUserResponse(user);
    }

    public async Task<PrivacySettingsDto> GetPrivacySettingsAsync(Guid userId)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId)
            ?? throw new ApiException("User not found", 404, "USER_NOT_FOUND");

        return ToPrivacySettingsDto(user);
    }

    public async Task<PrivacySettingsDto> UpdatePrivacySettingsAsync(Guid userId, PrivacySettingsDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId)
            ?? throw new ApiException("User not found", 404, "USER_NOT_FOUND");

        user.NotificationsEnabled = dto.NotificationsEnabled;
        user.MessageNotificationsEnabled = dto.MessageNotificationsEnabled;
        user.CommunityNotificationsEnabled = dto.CommunityNotificationsEnabled;
        user.EmailNotificationsEnabled = dto.EmailNotificationsEnabled;
        user.ProfileVisibility = NormalizeVisibility(dto.ProfileVisibility, "Public");
        user.PostsVisibility = NormalizeVisibility(dto.PostsVisibility, "Public");
        user.ShowOnlineStatus = dto.ShowOnlineStatus;
        user.ShowLastSeen = dto.ShowLastSeen;
        user.AllowMessageRequests = dto.AllowMessageRequests;
        user.AllowSearchByEmail = dto.AllowSearchByEmail;

        await _context.SaveChangesAsync();
        return ToPrivacySettingsDto(user);
    }

    public async Task<UserResponseDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId) ?? throw new ApiException("User not found", 404, "USER_NOT_FOUND");
        var newName = (dto.FullName ?? dto.Name)?.Trim(); if (!string.IsNullOrWhiteSpace(newName)) user.Name = newName;
        if (dto.Username != null)
        {
            var username = dto.Username.Trim();
            if (!string.IsNullOrWhiteSpace(username))
            {
                if (await _context.Users.AnyAsync(x => x.Id != userId && x.Username != null && x.Username.ToLower() == username.ToLower())) throw new ApiException("Username already exists", 409, "USERNAME_EXISTS");
                user.Username = username;
            }
            else user.Username = null;
        }
        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            if (await _context.Users.AnyAsync(x => x.Id != userId && x.Email == email)) throw new ApiException("Email already exists", 409, "EMAIL_EXISTS");
            if (email != user.Email)
            {
                user.Email = email;
                user.EmailConfirmed = false;
                user.EmailConfirmedAt = null;
                var activationToken = GenerateUrlSafeToken();
                user.EmailConfirmationTokenHash = BCrypt.Net.BCrypt.HashPassword(activationToken);
                user.EmailConfirmationTokenExpiresAt = DateTime.UtcNow.AddHours(24);
                user.LastEmailConfirmationSentAt = DateTime.UtcNow;
                var activationEmailSent = await SendActivationEmailAsync(user, activationToken);
                if (!activationEmailSent)
                {
                    if (CanBypassActivationEmailForDevelopment())
                    {
                        user.EmailConfirmed = true;
                        user.EmailConfirmedAt = DateTime.UtcNow;
                        user.EmailConfirmationTokenHash = null;
                        user.EmailConfirmationTokenExpiresAt = null;
                    }
                    else
                    {
                        throw new ApiException("Email was not changed because the activation email could not be sent. Check Email settings in appsettings.json.", 500, "EMAIL_SEND_FAILED");
                    }
                }
            }
        }
        if (dto.BirthDate.HasValue)
            user.BirthDate = AsUtc(dto.BirthDate.Value);
        if (dto.Age.HasValue) user.Age = dto.Age.Value; else if (dto.BirthDate.HasValue) user.Age = CalculateAge(dto.BirthDate.Value);
        if (dto.WeightKg.HasValue) user.WeightKg = dto.WeightKg.Value;
        if (dto.HeightCm.HasValue) user.HeightCm = dto.HeightCm.Value;
        if (dto.Goal != null) user.Goal = string.IsNullOrWhiteSpace(dto.Goal) ? null : dto.Goal.Trim();
        if (dto.Gender != null) user.Gender = string.IsNullOrWhiteSpace(dto.Gender) ? null : dto.Gender.Trim();
        if (dto.ActivityLevel != null) user.ActivityLevel = string.IsNullOrWhiteSpace(dto.ActivityLevel) ? null : dto.ActivityLevel.Trim();
        if (dto.Location != null) user.Location = string.IsNullOrWhiteSpace(dto.Location) ? null : dto.Location.Trim();
        if (dto.Phone != null) user.Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();
        if (dto.ProfileImage != null) user.ProfileImageUrl = string.IsNullOrWhiteSpace(dto.ProfileImage) ? null : dto.ProfileImage.Trim();
        await _context.SaveChangesAsync();
        return ToUserResponse(await _context.Users.AsNoTracking().FirstAsync(x => x.Id == userId));
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        if (dto.NewPassword != dto.ConfirmPassword) throw new ApiException("Passwords do not match", 400, "PASSWORD_MISMATCH");
        ValidatePasswordStrength(dto.NewPassword);
        var user = await _context.Users.FindAsync(userId) ?? throw new ApiException("User not found", 404, "USER_NOT_FOUND");
        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash)) throw new ApiException("Current password is incorrect", 400, "INVALID_CURRENT_PASSWORD");
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAccountAsync(Guid userId, DeleteAccountRequestDto? dto = null)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null) throw new ApiException("User not found", 404, "USER_NOT_FOUND");

        if (!string.Equals(dto?.ConfirmationText?.Trim(), "DELETE MY ACCOUNT", StringComparison.Ordinal))
            throw new ApiException("Please type DELETE MY ACCOUNT to confirm account deletion.", 400, "DELETE_CONFIRMATION_REQUIRED");

        if (user.AuthProvider == "Local" && !BCrypt.Net.BCrypt.Verify(dto?.Password ?? string.Empty, user.PasswordHash))
            throw new ApiException("Password is required to delete your account.", 400, "INVALID_CURRENT_PASSWORD");

        await using var tx = await _context.Database.BeginTransactionAsync();

        var userThreadIds = await _context.ChatParticipants
            .Where(x => x.UserId == userId)
            .Select(x => x.ThreadId)
            .Distinct()
            .ToListAsync();

        var medicationIds = await _context.Medications
            .Where(x => x.UserId == userId)
            .Select(x => x.Id)
            .ToListAsync();

        var ownPostIds = await _context.CommunityPosts
            .Where(x => x.UserId == userId)
            .Select(x => x.Id)
            .ToListAsync();

        var ownRecipeIds = await _context.Recipes
            .Where(x => x.AuthorId == userId)
            .Select(x => x.Id)
            .ToListAsync();

        var ownDietPlanIds = await _context.DietPlans
            .Where(x => x.CreatedBy == userId)
            .Select(x => x.Id)
            .ToListAsync();

        if (userThreadIds.Count > 0)
        {
            await _context.ChatMessages.Where(x => userThreadIds.Contains(x.ThreadId)).ExecuteDeleteAsync();
            await _context.ChatParticipants.Where(x => userThreadIds.Contains(x.ThreadId)).ExecuteDeleteAsync();
            await _context.ChatThreads.Where(x => userThreadIds.Contains(x.Id)).ExecuteDeleteAsync();
        }

        await _context.PasswordResetCodes.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        await _context.RefreshTokens.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        await _context.Notifications.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        await _context.UserBlocks.Where(x => x.BlockerId == userId || x.BlockedId == userId).ExecuteDeleteAsync();
        await _context.ContentReports.Where(x => x.ReporterId == userId || x.ReportedUserId == userId).ExecuteDeleteAsync();
        await _context.HiddenPosts.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        await _context.UserAllergies.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        await _context.UserDislikedFoods.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        await _context.WaterReminders.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        await _context.WaterLogs.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        await _context.WaterGoals.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        await _context.MealLogs.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        await _context.UserPlans.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        await _context.UserFollows.Where(x => x.FollowerId == userId || x.FollowingId == userId).ExecuteDeleteAsync();

        if (medicationIds.Count > 0)
            await _context.MedicationSchedules.Where(x => medicationIds.Contains(x.MedicationId)).ExecuteDeleteAsync();
        await _context.Medications.Where(x => x.UserId == userId).ExecuteDeleteAsync();

        if (ownPostIds.Count > 0)
        {
            await _context.CommunityPosts
                .Where(x => x.SharedPostId.HasValue && ownPostIds.Contains(x.SharedPostId.Value))
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.SharedPostId, (Guid?)null));
            await _context.HiddenPosts.Where(x => ownPostIds.Contains(x.PostId)).ExecuteDeleteAsync();
            await _context.Comments.Where(x => ownPostIds.Contains(x.PostId)).ExecuteDeleteAsync();
            await _context.PostLikes.Where(x => ownPostIds.Contains(x.PostId)).ExecuteDeleteAsync();
        }

        await _context.Comments.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        await _context.PostLikes.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        await _context.CommunityPosts.Where(x => x.UserId == userId).ExecuteDeleteAsync();

        await _context.RecipeSaved.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        if (ownRecipeIds.Count > 0)
        {
            await _context.DietPlanItems
                .Where(x => x.RecipeId.HasValue && ownRecipeIds.Contains(x.RecipeId.Value))
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.RecipeId, (Guid?)null));
            await _context.RecipeSaved.Where(x => ownRecipeIds.Contains(x.RecipeId)).ExecuteDeleteAsync();
        }
        await _context.Recipes.Where(x => x.AuthorId == userId).ExecuteDeleteAsync();

        if (ownDietPlanIds.Count > 0)
        {
            await _context.UserPlans.Where(x => ownDietPlanIds.Contains(x.PlanId)).ExecuteDeleteAsync();
            await _context.DietPlanItems.Where(x => ownDietPlanIds.Contains(x.PlanId)).ExecuteDeleteAsync();
        }
        await _context.DietPlans.Where(x => x.CreatedBy == userId).ExecuteDeleteAsync();

        await _context.Users.Where(x => x.Id == userId).ExecuteDeleteAsync();
        await tx.CommitAsync();
    }

    public static UserResponseDto ToUserResponse(User user) => new()
    {
        Id = user.Id, FullName = user.Name, Username = user.Username, Email = user.Email, EmailConfirmed = user.EmailConfirmed, Gender = user.Gender,
        BirthDate = user.BirthDate, Location = user.Location, Phone = user.Phone, ProfileImage = user.ProfileImageUrl,
        Age = user.Age, WeightKg = user.WeightKg, HeightCm = user.HeightCm, Goal = user.Goal, ActivityLevel = user.ActivityLevel, Role = user.Role,
        NotificationsEnabled = user.NotificationsEnabled, MessageNotificationsEnabled = user.MessageNotificationsEnabled,
        CommunityNotificationsEnabled = user.CommunityNotificationsEnabled, EmailNotificationsEnabled = user.EmailNotificationsEnabled,
        ProfileVisibility = user.ProfileVisibility, PostsVisibility = user.PostsVisibility, ShowOnlineStatus = user.ShowOnlineStatus,
        ShowLastSeen = user.ShowLastSeen, AllowMessageRequests = user.AllowMessageRequests, AllowSearchByEmail = user.AllowSearchByEmail
    };

    private static PrivacySettingsDto ToPrivacySettingsDto(User user) => new()
    {
        NotificationsEnabled = user.NotificationsEnabled,
        MessageNotificationsEnabled = user.MessageNotificationsEnabled,
        CommunityNotificationsEnabled = user.CommunityNotificationsEnabled,
        EmailNotificationsEnabled = user.EmailNotificationsEnabled,
        ProfileVisibility = user.ProfileVisibility,
        PostsVisibility = user.PostsVisibility,
        ShowOnlineStatus = user.ShowOnlineStatus,
        ShowLastSeen = user.ShowLastSeen,
        AllowMessageRequests = user.AllowMessageRequests,
        AllowSearchByEmail = user.AllowSearchByEmail
    };

    private static string NormalizeVisibility(string? value, string fallback)
    {
        var normalized = (value ?? fallback).Trim().ToLowerInvariant();
        return normalized switch
        {
            "friends" or "friends_only" or "friends-only" => "Friends",
            "private" or "only_me" or "only-me" => "Private",
            _ => "Public"
        };
    }

    private static void ValidatePasswordStrength(string? password)
    {
        var value = password ?? string.Empty;
        if (value.Length < 8 ||
            !value.Any(char.IsUpper) ||
            !value.Any(char.IsLower) ||
            !value.Any(char.IsDigit) ||
            !value.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            throw new ApiException("Password must be at least 8 characters and include uppercase, lowercase, number, and special character.", 400, "WEAK_PASSWORD");
        }
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

    private bool CanBypassActivationEmailForDevelopment()
    {
        var bypassValue = _configuration["Email:BypassWhenMissingInDevelopment"];
        var bypassEnabled = string.IsNullOrWhiteSpace(bypassValue) || bool.TryParse(bypassValue, out var parsed) && parsed;
        var password = _configuration["Email:Password"];

        return bypassEnabled &&
               (string.IsNullOrWhiteSpace(password) ||
                password.Contains("PUT_GMAIL_APP_PASSWORD_HERE", StringComparison.OrdinalIgnoreCase) ||
                password.Contains("APP_PASSWORD", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<bool> SendActivationEmailAsync(User user, string activationToken)
    {
        var activationUrl = BuildActivationUrl(user.Email, activationToken);
        var body =
            $"Hi {user.Name},\n\n" +
            "Welcome to Eatopia. Please activate your account using this link:\n" +
            $"{activationUrl}\n\n" +
            "This link expires in 24 hours. If you did not create an account, ignore this email.";

        return await _emailService.SendAsync(user.Email, "Activate your Eatopia account", body);
    }

    private string BuildActivationUrl(string email, string token)
    {
        var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? Environment.GetEnvironmentVariable("FRONTEND_URL");
        if (string.IsNullOrWhiteSpace(frontendBaseUrl))
        {
            throw new InvalidOperationException("Frontend:BaseUrl is missing. Set Frontend__BaseUrl or FRONTEND_URL.");
        }

        frontendBaseUrl = frontendBaseUrl.TrimEnd('/');

        return $"{frontendBaseUrl}/activate-account?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
    }

    private static string GenerateUrlSafeToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", string.Empty);
    }

    private async Task<string> BuildUniqueUsernameFromEmailAsync(string email)
    {
        var baseName = email.Split('@')[0].Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(baseName)) baseName = ($"user{Guid.NewGuid():N}")[..12];

        var candidate = baseName;
        var suffix = 1;
        while (await _context.Users.AnyAsync(x => x.Username != null && x.Username.ToLower() == candidate.ToLower()))
        {
            candidate = $"{baseName}{suffix}";
            suffix += 1;
        }

        return candidate;
    }
    private static DateTime AsUtc(DateTime value) => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
    private static int CalculateAge(DateTime birthDate) { var today = DateTime.UtcNow.Date; var age = today.Year - birthDate.Date.Year; if (birthDate.Date > today.AddYears(-age)) age--; return Math.Max(age, 0); }
}

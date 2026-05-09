using Eatopia.Application.DTOs.AI;
using Eatopia.Application.Exceptions;
using Eatopia.Application.Interfaces;
using Eatopia.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Eatopia.Api.Controllers;

[ApiController]
[Route("api/ai")]
[Route("api/v1/ai")]
public class AiController : ControllerBase
{
    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private readonly IFoodAiClient _aiClient;
    private readonly EatopiaDbContext _context;

    public AiController(IFoodAiClient aiClient, EatopiaDbContext context)
    {
        _aiClient = aiClient;
        _context = context;
    }

    [Authorize]
    [HttpPost("diet-plan")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateDietPlan([FromBody] GenerateFrontendDietPlanRequestDto dto, CancellationToken cancellationToken)
    {
        NormalizeDietGoalFromPreferences(dto);
        await EnrichDietPlanRequestFromProfileAsync(dto);
        NormalizeDietGoalFromPreferences(dto);

        var missingFields = GetMissingDietPlanFields(dto);
        if (missingFields.Count > 0)
        {
            return BadRequest(new
            {
                success = false,
                message = "Complete your health profile before generating a diet plan.",
                missingFields
            });
        }

        var response = await _aiClient.GenerateDietPlanAsync(dto, cancellationToken);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("scan")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(8 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 8 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ScanMeal([FromForm] IFormFile image, CancellationToken cancellationToken)
    {
        if (image == null || image.Length == 0)
            return BadRequest(new { success = false, message = "No image uploaded." });

        if (!image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { success = false, message = "Uploaded file must be an image." });

        var extension = Path.GetExtension(image.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
            return BadRequest(new { success = false, message = "Supported images are JPG, PNG, and WebP." });

        try
        {
            await using var stream = image.OpenReadStream();
            var result = await _aiClient.AnalyzeFoodImageAsync(
                stream,
                image.FileName,
                image.ContentType,
                cancellationToken);

            return Ok(new { success = true, result, data = result });
        }
        catch (ApiException ex)
        {
            return StatusCode(ex.StatusCode, new
            {
                success = false,
                message = ex.Message,
                error = new
                {
                    code = ex.Code,
                    message = ex.Message
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                success = false,
                message = $"AI scan failed: {ex.Message}",
                error = new
                {
                    code = "AI_SCAN_FAILED",
                    message = ex.Message
                }
            });
        }
    }

    private async Task EnrichDietPlanRequestFromProfileAsync(GenerateFrontendDietPlanRequestDto dto)
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(id) || !Guid.TryParse(id, out var userId))
            return;

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null)
            return;

        dto.Age ??= user.Age ?? (user.BirthDate.HasValue ? CalculateAge(user.BirthDate.Value) : null);
        dto.WeightKg ??= user.WeightKg;
        dto.HeightCm ??= user.HeightCm;
        dto.Goal = string.IsNullOrWhiteSpace(dto.Goal) ? user.Goal : dto.Goal;
        dto.ActivityLevel = string.IsNullOrWhiteSpace(dto.ActivityLevel) ? user.ActivityLevel : dto.ActivityLevel;

        dto.Preferences ??= new FrontendDietPlanPreferencesDto();
        if (string.IsNullOrWhiteSpace(dto.Preferences.Goal))
            dto.Preferences.Goal = dto.Goal;
    }

    private static void NormalizeDietGoalFromPreferences(GenerateFrontendDietPlanRequestDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.Goal))
            return;

        if (!string.IsNullOrWhiteSpace(dto.Preferences?.Goal))
            dto.Goal = dto.Preferences.Goal;
    }

    private static List<string> GetMissingDietPlanFields(GenerateFrontendDietPlanRequestDto dto)
    {
        var missing = new List<string>();

        if (!dto.Age.HasValue || dto.Age.Value <= 0)
            missing.Add("age");
        if (!dto.HeightCm.HasValue || dto.HeightCm.Value <= 0)
            missing.Add("height");
        if (!dto.WeightKg.HasValue || dto.WeightKg.Value <= 0)
            missing.Add("weight");
        if (string.IsNullOrWhiteSpace(dto.Goal))
            missing.Add("goal");
        if (string.IsNullOrWhiteSpace(dto.ActivityLevel))
            missing.Add("activityLevel");

        return missing;
    }

    private static int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.UtcNow.Date;
        var age = today.Year - birthDate.Date.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return Math.Max(age, 0);
    }
}

using Eatopia.Application.DTOs.AI;
using Eatopia.Application.Interfaces;

namespace Eatopia.Infrastructure.AI;

public class FakeFoodAiClient : IFoodAiClient
{
    public Task<AiFoodResultDto> AnalyzeFoodImageAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        return AnalyzeFoodImageAsyncCore();
    }

    public Task<AiFoodResultDto> AnalyzeFoodImageAsync(Stream imageStream, string fileName, string? contentType, CancellationToken cancellationToken = default)
    {
        return AnalyzeFoodImageAsyncCore();
    }

    private static Task<AiFoodResultDto> AnalyzeFoodImageAsyncCore()
    {
        // TEMP for demo: return a constant prediction.
        // Replace this with a real AI call later (Python model / Azure ML / external API).
        return Task.FromResult(new AiFoodResultDto
        {
            FoodName = "Pizza",
            Confidence = 0.90m
        });
    }

    public Task<FrontendDietPlanResponseDto> GenerateDietPlanAsync(GenerateFrontendDietPlanRequestDto dto, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new FrontendDietPlanResponseDto());
    }
}

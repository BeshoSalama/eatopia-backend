using Eatopia.Application.DTOs.AI;
using Eatopia.Application.Interfaces;

namespace Eatopia.Infrastructure.AI;

public class FakeFoodAiClient : IFoodAiClient
{
    public Task<AiFoodResultDto> AnalyzeFoodImageAsync(string imageUrl)
    {
        // TEMP for demo: return a constant prediction.
        // Replace this with a real AI call later (Python model / Azure ML / external API).
        return Task.FromResult(new AiFoodResultDto
        {
            FoodName = "Pizza",
            Confidence = 0.90m
        });
    }

    public Task<FrontendDietPlanResponseDto> GenerateDietPlanAsync(GenerateFrontendDietPlanRequestDto dto)
    {
        return Task.FromResult(new FrontendDietPlanResponseDto());
    }
}

using Eatopia.Application.DTOs.AI;

namespace Eatopia.Application.Interfaces;

public interface IFoodAiClient
{
    Task<AiFoodResultDto> AnalyzeFoodImageAsync(string imageUrl);
    Task<FrontendDietPlanResponseDto> GenerateDietPlanAsync(GenerateFrontendDietPlanRequestDto dto);
}

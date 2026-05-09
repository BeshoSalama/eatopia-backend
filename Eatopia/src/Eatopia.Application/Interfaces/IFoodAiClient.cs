using Eatopia.Application.DTOs.AI;

namespace Eatopia.Application.Interfaces;

public interface IFoodAiClient
{
    Task<AiFoodResultDto> AnalyzeFoodImageAsync(string imageUrl, CancellationToken cancellationToken = default);
    Task<AiFoodResultDto> AnalyzeFoodImageAsync(Stream imageStream, string fileName, string? contentType, CancellationToken cancellationToken = default);
    Task<FrontendDietPlanResponseDto> GenerateDietPlanAsync(GenerateFrontendDietPlanRequestDto dto, CancellationToken cancellationToken = default);
}

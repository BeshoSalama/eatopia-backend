using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Eatopia.Application.DTOs.DietPlans;

public class AssignUserPlanDto
{
    // Optional: if provided and caller is Admin, assign to that user.
    [JsonPropertyName("userId")]
    public Guid? UserId { get; set; }

    [Required]
    [JsonPropertyName("planId")]
    public Guid PlanId { get; set; }

    [Required]
    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }

    [Required]
    [JsonPropertyName("endDate")]
    public DateTime EndDate { get; set; }
}

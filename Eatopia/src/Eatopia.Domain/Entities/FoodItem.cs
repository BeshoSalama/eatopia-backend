using Eatopia.Domain.Common;

namespace Eatopia.Domain.Entities;

public class FoodItem : BaseEntity
{
    public string Name { get; set; } = null!;

    public decimal CaloriesPer100g { get; set; }
    public decimal ProteinPer100g { get; set; }
    public decimal FatPer100g { get; set; }
    public decimal CarbsPer100g { get; set; }

    public string? ServingSize { get; set; }
}

using Eatopia.Domain.Common;

namespace Eatopia.Domain.Entities;

public class Recipe : BaseEntity
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }

    public decimal? CaloriesPerServing { get; set; }
    public int Servings { get; set; }

    public string IngredientsJson { get; set; } = null!;
    public string StepsJson { get; set; } = null!;

    public Guid? AuthorId { get; set; }
    public User? Author { get; set; }
}

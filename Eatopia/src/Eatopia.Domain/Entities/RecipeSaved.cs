using Eatopia.Domain.Common;

namespace Eatopia.Domain.Entities;

public class RecipeSaved : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;
}

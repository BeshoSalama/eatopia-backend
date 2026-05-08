using Eatopia.Domain.Common;

namespace Eatopia.Domain.Entities;

public class UserDislikedFood : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string FoodName { get; set; } = null!;
}

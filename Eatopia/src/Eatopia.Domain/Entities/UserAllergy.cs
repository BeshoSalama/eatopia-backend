using Eatopia.Domain.Common;

namespace Eatopia.Domain.Entities;

public class UserAllergy : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string AllergyName { get; set; } = null!;
}

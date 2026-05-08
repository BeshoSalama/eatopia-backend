using Eatopia.Domain.Common;

namespace Eatopia.Domain.Entities;

public class Medication : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string? DosageText { get; set; }
    public string? BeforeAfterMeal { get; set; }

    public int TimesPerDay { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

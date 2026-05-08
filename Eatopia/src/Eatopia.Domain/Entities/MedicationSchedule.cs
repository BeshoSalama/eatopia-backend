using Eatopia.Domain.Common;

namespace Eatopia.Domain.Entities;

public class MedicationSchedule : BaseEntity
{
    public Guid MedicationId { get; set; }
    public Medication Medication { get; set; } = null!;

    public DateTime ScheduledDate { get; set; } // date-only
    public TimeSpan TimeOfDay { get; set; }

    public bool IsTaken { get; set; } = false;
    public DateTime? TakenAt { get; set; }
}

using Eatopia.Application.DTOs.Medication;
using Eatopia.Application.Exceptions;
using Eatopia.Domain.Entities;
using Eatopia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Eatopia.Infrastructure.Services;

public class MedicationService
{
    private readonly EatopiaDbContext _context;
    private readonly NotificationService _notificationService;

    public MedicationService(EatopiaDbContext context, NotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<object> CreateMedicationAsync(Guid userId, CreateMedicationDto dto)
    {
        if (dto.TimesPerDay <= 0)
            throw new ApiException("TimesPerDay must be > 0", 400, "VALIDATION_ERROR");

        if (dto.TimesOfDay == null || dto.TimesOfDay.Count == 0)
            throw new ApiException("TimesOfDay is required", 400, "VALIDATION_ERROR");

        if (dto.TimesOfDay.Count != dto.TimesPerDay)
            throw new ApiException("TimesOfDay count must match TimesPerDay", 400, "VALIDATION_ERROR");

        if (dto.EndDate.Date < dto.StartDate.Date)
            throw new ApiException("EndDate must be >= StartDate", 400, "VALIDATION_ERROR");

        var medication = new Medication
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = dto.Name.Trim(),
            DosageText = dto.DosageText,
            BeforeAfterMeal = NormalizeMealTiming(dto.BeforeAfterMeal),
            TimesPerDay = dto.TimesPerDay,
            StartDate = dto.StartDate.Date,
            EndDate = dto.EndDate.Date,
            CreatedAt = DateTime.UtcNow
        };

        _context.Medications.Add(medication);
        AddSchedules(medication.Id, dto.StartDate.Date, dto.EndDate.Date, dto.TimesOfDay);

        await _notificationService.CreateSystemNotificationAsync(
            userId,
            "Medication reminder added",
            $"{medication.Name} was added with {dto.TimesPerDay} reminder(s) per day.",
            "medication",
            sendEmail: true,
            relatedType: "Medication",
            relatedId: medication.Id);

        await _context.SaveChangesAsync();
        return await GetMedicationDetailsAsync(userId, medication.Id);
    }

    public async Task<List<object>> GetUserMedicationsAsync(Guid userId)
    {
        var medications = await _context.Medications
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var ids = medications.Select(x => x.Id).ToList();
        var schedules = await _context.MedicationSchedules
            .AsNoTracking()
            .Where(x => ids.Contains(x.MedicationId))
            .OrderBy(x => x.TimeOfDay)
            .ToListAsync();

        return medications.Select(m => ToFrontendMedication(m, schedules.Where(s => s.MedicationId == m.Id).ToList())).ToList();
    }

    public async Task<object> GetMedicationDetailsAsync(Guid userId, Guid medicationId)
    {
        var medication = await _context.Medications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == medicationId && x.UserId == userId);
        if (medication == null)
            throw new ApiException("Medication not found", 404, "NOT_FOUND");

        var schedules = await _context.MedicationSchedules.AsNoTracking()
            .Where(x => x.MedicationId == medicationId)
            .OrderBy(x => x.TimeOfDay)
            .ToListAsync();

        return ToFrontendMedication(medication, schedules);
    }

    public async Task<object> UpdateMedicationAsync(Guid userId, Guid medicationId, CreateMedicationDto dto)
    {
        var medication = await _context.Medications.FirstOrDefaultAsync(x => x.Id == medicationId && x.UserId == userId);
        if (medication == null)
            throw new ApiException("Medication not found", 404, "NOT_FOUND");

        if (dto.TimesOfDay == null || dto.TimesOfDay.Count != dto.TimesPerDay)
            throw new ApiException("TimesOfDay count must match TimesPerDay", 400, "VALIDATION_ERROR");

        medication.Name = dto.Name.Trim();
        medication.DosageText = dto.DosageText;
        medication.BeforeAfterMeal = NormalizeMealTiming(dto.BeforeAfterMeal);
        medication.TimesPerDay = dto.TimesPerDay;
        medication.StartDate = dto.StartDate.Date;
        medication.EndDate = dto.EndDate.Date;

        var oldSchedules = await _context.MedicationSchedules.Where(x => x.MedicationId == medicationId).ToListAsync();
        _context.MedicationSchedules.RemoveRange(oldSchedules);
        AddSchedules(medication.Id, dto.StartDate.Date, dto.EndDate.Date, dto.TimesOfDay);

        await _context.SaveChangesAsync();
        return await GetMedicationDetailsAsync(userId, medicationId);
    }

    public async Task DeleteMedicationAsync(Guid userId, Guid medicationId)
    {
        var medication = await _context.Medications.FirstOrDefaultAsync(x => x.Id == medicationId && x.UserId == userId);
        if (medication == null)
            throw new ApiException("Medication not found", 404, "NOT_FOUND");

        var schedules = await _context.MedicationSchedules.Where(x => x.MedicationId == medicationId).ToListAsync();
        _context.MedicationSchedules.RemoveRange(schedules);
        _context.Medications.Remove(medication);
        await _context.SaveChangesAsync();
    }

    public async Task<List<object>> GetTodaySchedulesAsync(Guid userId)
    {
        var today = DateTime.UtcNow.Date;

        var schedules = await _context.MedicationSchedules
            .Include(x => x.Medication)
            .Where(x => x.Medication.UserId == userId && x.ScheduledDate == today)
            .OrderBy(x => x.TimeOfDay)
            .ToListAsync();

        return schedules.Select(s => (object)new
        {
            s.Id,
            s.ScheduledDate,
            s.TimeOfDay,
            s.IsTaken,
            s.TakenAt,
            Medication = new
            {
                s.Medication.Id,
                s.Medication.Name,
                s.Medication.DosageText,
                s.Medication.BeforeAfterMeal,
                s.Medication.TimesPerDay
            }
        }).ToList();
    }

    public async Task<object> MarkDoseTakenAsync(Guid userId, Guid scheduleId, MarkDoseTakenDto dto)
    {
        var schedule = await _context.MedicationSchedules
            .Include(x => x.Medication)
            .FirstOrDefaultAsync(x => x.Id == scheduleId);

        if (schedule == null)
            throw new ApiException("Schedule not found", 404, "NOT_FOUND");

        if (schedule.Medication.UserId != userId)
            throw new ApiException("Unauthorized", 403, "FORBIDDEN");

        schedule.IsTaken = dto.IsTaken;
        schedule.TakenAt = dto.IsTaken ? (dto.TakenAt ?? DateTime.UtcNow) : null;

        await _context.SaveChangesAsync();

        return new
        {
            schedule.Id,
            schedule.ScheduledDate,
            schedule.TimeOfDay,
            schedule.IsTaken,
            schedule.TakenAt
        };
    }

    private void AddSchedules(Guid medicationId, DateTime startDate, DateTime endDate, List<TimeSpan> timesOfDay)
    {
        var current = startDate.Date;
        while (current <= endDate.Date)
        {
            foreach (var time in timesOfDay)
            {
                _context.MedicationSchedules.Add(new MedicationSchedule
                {
                    Id = Guid.NewGuid(),
                    MedicationId = medicationId,
                    ScheduledDate = current,
                    TimeOfDay = time,
                    IsTaken = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
            current = current.AddDays(1);
        }
    }

    private static string? NormalizeMealTiming(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        return normalized.Equals("After", StringComparison.OrdinalIgnoreCase) ? "After" : "Before";
    }

    private static object ToFrontendMedication(Medication med, List<MedicationSchedule> schedules)
    {
        var today = DateTime.UtcNow.Date;
        var todaySchedules = schedules.Where(x => x.ScheduledDate.Date == today).OrderBy(x => x.TimeOfDay).ToList();
        var displaySchedules = todaySchedules.Count > 0 ? todaySchedules : schedules
            .GroupBy(x => x.TimeOfDay)
            .Select(g => g.OrderBy(x => x.ScheduledDate).First())
            .OrderBy(x => x.TimeOfDay)
            .ToList();

        return new
        {
            id = med.Id,
            name = med.Name,
            dosageText = med.DosageText,
            beforeAfterMeal = med.BeforeAfterMeal ?? "Before",
            timesPerDay = med.TimesPerDay,
            startDate = med.StartDate,
            endDate = med.EndDate,
            schedule = displaySchedules.Select(x => x.TimeOfDay.ToString(@"hh\:mm")).ToList(),
            scheduleIds = displaySchedules.Select(x => x.Id).ToList(),
            done = displaySchedules.Select(x => x.IsTaken).ToList()
        };
    }
}

using Eatopia.Application.DTOs.Water;
using Eatopia.Application.Exceptions;
using Eatopia.Domain.Entities;
using Eatopia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Eatopia.Infrastructure.Services;

public class WaterService
{
    private readonly EatopiaDbContext _context;
    private readonly NotificationService _notificationService;

    public WaterService(EatopiaDbContext context, NotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<WaterGoal?> GetGoalAsync(Guid userId)
    {
        return await _context.WaterGoals.FirstOrDefaultAsync(x => x.UserId == userId);
    }

    public async Task<WaterGoal> UpdateGoalAsync(Guid userId, UpdateWaterGoalDto dto)
    {
        if (dto.DailyTargetMl <= 0)
            throw new ApiException("DailyTargetMl must be > 0", 400, "VALIDATION_ERROR");

        if (dto.RemindEveryMinutes <= 0)
            throw new ApiException("RemindEveryMinutes must be > 0", 400, "VALIDATION_ERROR");

        var goal = await _context.WaterGoals.FirstOrDefaultAsync(x => x.UserId == userId);

        if (goal == null)
        {
            goal = new WaterGoal
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DailyTargetMl = dto.DailyTargetMl,
                RemindEveryMinutes = dto.RemindEveryMinutes,
                CreatedAt = DateTime.UtcNow
            };

            _context.WaterGoals.Add(goal);
        }
        else
        {
            goal.DailyTargetMl = dto.DailyTargetMl;
            goal.RemindEveryMinutes = dto.RemindEveryMinutes;
        }

        await _context.SaveChangesAsync();
        return goal;
    }

    public async Task<WaterLog> AddLogAsync(Guid userId, AddWaterLogDto dto)
    {
        if (dto.AmountMl <= 0)
            throw new ApiException("AmountMl must be > 0", 400, "VALIDATION_ERROR");

        var log = new WaterLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AmountMl = dto.AmountMl,
            LoggedAt = dto.LoggedAt == default ? DateTime.UtcNow : dto.LoggedAt,
            CreatedAt = DateTime.UtcNow
        };

        _context.WaterLogs.Add(log);
        await _context.SaveChangesAsync();

        return log;
    }

    public async Task<object> GetLogsByDateAsync(Guid userId, DateTime date)
    {
        var day = date.Date;

        var logs = await _context.WaterLogs
            .Where(x => x.UserId == userId && x.LoggedAt.Date == day)
            .OrderByDescending(x => x.LoggedAt)
            .ToListAsync();

        var total = logs.Sum(x => x.AmountMl);
        var goal = await _context.WaterGoals.FirstOrDefaultAsync(x => x.UserId == userId);

        return new
        {
            date = day,
            totalDrankMl = total,
            targetMl = goal?.DailyTargetMl ?? 0,
            logs
        };
    }

    public async Task<List<object>> GetRemindersAsync(Guid userId, DateTime? date = null)
    {
        var day = (date ?? DateTime.Now).Date;
        await EnsureDefaultRemindersAsync(userId, day);

        var reminders = await _context.WaterReminders
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.ReminderDate == day)
            .OrderBy(x => x.TimeOfDay)
            .ToListAsync();

        return reminders.Select(ToFrontendReminder).ToList();
    }

    public async Task<List<object>> UpsertRemindersAsync(Guid userId, UpsertWaterRemindersDto dto)
    {
        var day = (dto.Date ?? DateTime.Now).Date;
        var existing = await _context.WaterReminders.Where(x => x.UserId == userId && x.ReminderDate == day).ToListAsync();

        _context.WaterReminders.RemoveRange(existing);

        foreach (var item in dto.Intakes.OrderBy(x => x.TimeOfDay))
        {
            _context.WaterReminders.Add(new WaterReminder
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ReminderDate = day,
                TimeOfDay = item.TimeOfDay,
                AmountMl = item.AmountMl,
                IsCompleted = item.IsCompleted,
                CompletedAt = item.IsCompleted ? DateTime.UtcNow : null,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _notificationService.CreateSystemNotificationAsync(
            userId,
            "Water reminders updated",
            $"Your daily water plan was saved with {dto.Intakes.Count} reminder(s).",
            "water",
            sendEmail: false,
            relatedType: "WaterReminder");

        await _context.SaveChangesAsync();
        return await GetRemindersAsync(userId, day);
    }

    public async Task<object> ToggleReminderAsync(Guid userId, Guid reminderId, bool isCompleted)
    {
        var reminder = await _context.WaterReminders.FirstOrDefaultAsync(x => x.Id == reminderId && x.UserId == userId);
        if (reminder == null)
            throw new ApiException("Water reminder not found", 404, "NOT_FOUND");

        reminder.IsCompleted = isCompleted;
        reminder.CompletedAt = isCompleted ? DateTime.UtcNow : null;

        if (isCompleted)
        {
            _context.WaterLogs.Add(new WaterLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AmountMl = reminder.AmountMl,
                LoggedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        return ToFrontendReminder(reminder);
    }

    private async Task EnsureDefaultRemindersAsync(Guid userId, DateTime day)
    {
        var exists = await _context.WaterReminders.AnyAsync(x => x.UserId == userId && x.ReminderDate == day);
        if (exists)
            return;

        var defaults = new[]
        {
            ("08:00", 500), ("10:30", 500), ("12:30", 500),
            ("15:00", 500), ("17:30", 500), ("21:00", 500)
        };

        foreach (var (time, amount) in defaults)
        {
            _context.WaterReminders.Add(new WaterReminder
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ReminderDate = day,
                TimeOfDay = TimeSpan.Parse(time),
                AmountMl = amount,
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();
    }

    private static object ToFrontendReminder(WaterReminder reminder) => new
    {
        id = reminder.Id,
        timeOfDay = reminder.TimeOfDay.ToString(@"hh\:mm"),
        time = FormatDisplayTime(reminder.TimeOfDay),
        amount = reminder.AmountMl,
        done = reminder.IsCompleted,
        reminderDate = reminder.ReminderDate,
        completedAt = reminder.CompletedAt
    };

    private static string FormatDisplayTime(TimeSpan time)
    {
        var date = DateTime.Today.Add(time);
        return date.ToString("h:mm tt", System.Globalization.CultureInfo.InvariantCulture);
    }
}

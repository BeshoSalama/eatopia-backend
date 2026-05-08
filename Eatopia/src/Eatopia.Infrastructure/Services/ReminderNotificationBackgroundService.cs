using Eatopia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Eatopia.Infrastructure.Services;

public class ReminderNotificationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReminderNotificationBackgroundService> _logger;

    public ReminderNotificationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ReminderNotificationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run once shortly after the API starts, then every 30 seconds.
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueRemindersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Reminder notification background check failed.");
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    private async Task ProcessDueRemindersAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<EatopiaDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();

        // These reminders are entered by the user in the local UI time.
        // DateTime.UtcNow keeps local development behavior intuitive.
        var now = DateTime.UtcNow;
        var today = now.Date;
        var currentTime = now.TimeOfDay;

        await SendWaterRemindersAsync(db, notificationService, today, currentTime, cancellationToken);
        await SendMedicationRemindersAsync(db, notificationService, today, currentTime, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SendWaterRemindersAsync(
        EatopiaDbContext db,
        NotificationService notificationService,
        DateTime today,
        TimeSpan currentTime,
        CancellationToken cancellationToken)
    {
        var dueWaterReminders = await db.WaterReminders
            .AsNoTracking()
            .Where(x =>
                x.ReminderDate == today &&
                !x.IsCompleted &&
                x.TimeOfDay <= currentTime)
            .OrderBy(x => x.TimeOfDay)
            .ToListAsync(cancellationToken);

        foreach (var reminder in dueWaterReminders)
        {
            var existingNotification = await db.Notifications.FirstOrDefaultAsync(x =>
                x.UserId == reminder.UserId &&
                x.RelatedEntityType == "WaterReminder" &&
                x.RelatedEntityId == reminder.Id,
                cancellationToken);

            if (existingNotification != null)
            {
                if (!existingNotification.EmailSent)
                    await notificationService.TrySendNotificationEmailAsync(existingNotification.Id);
                continue;
            }

            var scheduledFor = today.Add(reminder.TimeOfDay);

            await notificationService.CreateSystemNotificationAsync(
                reminder.UserId,
                "Water Reminder",
                $"It's time to drink {reminder.AmountMl}ml of water.",
                "water",
                sendEmail: true,
                relatedType: "WaterReminder",
                relatedId: reminder.Id,
                scheduledFor: scheduledFor);
        }
    }

    private static async Task SendMedicationRemindersAsync(
        EatopiaDbContext db,
        NotificationService notificationService,
        DateTime today,
        TimeSpan currentTime,
        CancellationToken cancellationToken)
    {
        var dueMedicationSchedules = await db.MedicationSchedules
            .AsNoTracking()
            .Include(x => x.Medication)
            .Where(x =>
                x.ScheduledDate == today &&
                !x.IsTaken &&
                x.TimeOfDay <= currentTime)
            .OrderBy(x => x.TimeOfDay)
            .ToListAsync(cancellationToken);

        foreach (var schedule in dueMedicationSchedules)
        {
            var existingNotification = await db.Notifications.FirstOrDefaultAsync(x =>
                x.UserId == schedule.Medication.UserId &&
                x.RelatedEntityType == "MedicationSchedule" &&
                x.RelatedEntityId == schedule.Id,
                cancellationToken);

            if (existingNotification != null)
            {
                if (!existingNotification.EmailSent)
                    await notificationService.TrySendNotificationEmailAsync(existingNotification.Id);
                continue;
            }

            var medication = schedule.Medication;
            var scheduledFor = today.Add(schedule.TimeOfDay);
            var mealTiming = string.IsNullOrWhiteSpace(medication.BeforeAfterMeal)
                ? ""
                : $" ({medication.BeforeAfterMeal} meal)";

            await notificationService.CreateSystemNotificationAsync(
                medication.UserId,
                "Medication Reminder",
                $"It's time to take {medication.Name}{mealTiming}.",
                "medication",
                sendEmail: true,
                relatedType: "MedicationSchedule",
                relatedId: schedule.Id,
                scheduledFor: scheduledFor);
        }
    }
}

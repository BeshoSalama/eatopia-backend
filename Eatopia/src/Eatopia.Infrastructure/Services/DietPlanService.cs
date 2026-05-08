using Eatopia.Application.DTOs.DietPlans;
using Eatopia.Application.Exceptions;
using Eatopia.Domain.Entities;
using Eatopia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Eatopia.Infrastructure.Services;

public class DietPlanService
{
    private readonly EatopiaDbContext _context;

    public DietPlanService(EatopiaDbContext context)
    {
        _context = context;
    }

    public async Task<DietPlan> CreatePlanAsync(Guid userId, CreateDietPlanDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new ApiException("Title is required", 400, "VALIDATION_ERROR");

        if (dto.DurationDays <= 0)
            throw new ApiException("DurationDays must be > 0", 400, "VALIDATION_ERROR");

        var plan = new DietPlan
        {
            Id = Guid.NewGuid(),
            Title = dto.Title.Trim(),
            CaloriesTargetPerDay = dto.CaloriesTargetPerDay,
            DurationDays = dto.DurationDays,
            CreatedBy = userId
        };

        _context.DietPlans.Add(plan);
        await _context.SaveChangesAsync();

        return plan;
    }

    public async Task<DietPlan> GeneratePlanAsync(Guid userId, GenerateDietPlanDto dto)
    {
        if (dto.DurationDays <= 0)
            throw new ApiException("DurationDays must be > 0", 400, "VALIDATION_ERROR");

        if (dto.CaloriesTargetPerDay <= 0)
            throw new ApiException("CaloriesTargetPerDay must be > 0", 400, "VALIDATION_ERROR");

        var plan = new DietPlan
        {
            Id = Guid.NewGuid(),
            Title = $"Generated Plan ({dto.Goal ?? "general"})",
            CaloriesTargetPerDay = dto.CaloriesTargetPerDay,
            DurationDays = dto.DurationDays,
            CreatedBy = userId
        };

        _context.DietPlans.Add(plan);

        var mealTemplates = new Dictionary<string, (decimal Share, string[] Titles)>
        {
            ["Breakfast"] = (0.25m, new[]
            {
                "Ful Medames Bowl",
                "Egyptian Egg Tomato Skillet",
                "Greek Yogurt Fruit Cup",
                "Oat Banana Bowl",
                "Low-Fat Cheese Baladi Plate",
                "Dates Yogurt Cup",
                "Fava Bean Toast"
            }),
            ["Lunch"] = (0.35m, new[]
            {
                "Light Koshari Bowl",
                "Molokhia Chicken Bowl",
                "Sayadeya Fish Plate",
                "Vegetable Torly Bowl",
                "Lentil Rice Plate",
                "Grilled Kofta Salad Plate",
                "Chicken Shawarma Rice Bowl"
            }),
            ["Dinner"] = (0.30m, new[]
            {
                "Eggplant Mesakaa Plate",
                "Taameya Salad Plate",
                "Tuna Baladi Salad",
                "Lentil Soup with Lemon",
                "Grilled Chicken Salad",
                "Roasted Vegetable Fatteh",
                "Stuffed Pepper Plate"
            }),
            ["Snack"] = (0.10m, new[]
            {
                "Roasted Chickpeas Snack",
                "Apple Cinnamon Yogurt",
                "Cucumber Cheese Bites",
                "Orange and Nuts",
                "Banana Tahini Bite",
                "Carrot Sticks with Hummus",
                "Dates and Milk Cup"
            })
        };

        for (var day = 1; day <= dto.DurationDays; day++)
        {
            foreach (var (mealType, template) in mealTemplates)
            {
                _context.DietPlanItems.Add(new DietPlanItem
                {
                    Id = Guid.NewGuid(),
                    PlanId = plan.Id,
                    DayOfWeek = day,
                    MealType = mealType,
                    Title = template.Titles[(day - 1) % template.Titles.Length],
                    CaloriesEstimated = dto.CaloriesTargetPerDay * template.Share
                });
            }
        }

        await _context.SaveChangesAsync();
        return plan;
    }

    public async Task<UserPlan> AssignPlanToUserAsync(Guid userId, AssignUserPlanDto dto)
    {
        var plan = await _context.DietPlans.FindAsync(dto.PlanId);
        if (plan == null)
            throw new ApiException("Plan not found", 404, "NOT_FOUND");

        if (dto.EndDate.Date < dto.StartDate.Date)
            throw new ApiException("EndDate must be >= StartDate", 400, "VALIDATION_ERROR");

        var userPlan = new UserPlan
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = dto.PlanId,
            StartDate = dto.StartDate.Date,
            EndDate = dto.EndDate.Date
        };

        _context.UserPlans.Add(userPlan);
        await _context.SaveChangesAsync();

        return userPlan;
    }

    public async Task<object?> GetActivePlanAsync(Guid userId)
    {
        var today = DateTime.UtcNow.Date;

        var active = await _context.UserPlans
            .Include(x => x.Plan)
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.StartDate <= today &&
                x.EndDate >= today);

        if (active == null)
            return null;

        var items = await _context.DietPlanItems
            .Where(x => x.PlanId == active.PlanId)
            .OrderBy(x => x.DayOfWeek)
            .ThenBy(x => x.MealType)
            .ToListAsync();

        return new
        {
            active.Id,
            active.StartDate,
            active.EndDate,
            Plan = new
            {
                active.Plan.Id,
                active.Plan.Title,
                active.Plan.CaloriesTargetPerDay,
                active.Plan.DurationDays,
                Items = items.Select(i => new
                {
                    i.Id,
                    i.DayOfWeek,
                    i.MealType,
                    i.Title,
                    i.RecipeId,
                    i.CaloriesEstimated
                })
            }
        };
    }
}

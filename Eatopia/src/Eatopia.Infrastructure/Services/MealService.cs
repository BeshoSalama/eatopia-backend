using Eatopia.Application.Common;
using Eatopia.Application.DTOs.Meals;
using Eatopia.Application.Exceptions;
using Eatopia.Application.Interfaces;
using Eatopia.Domain.Entities;
using Eatopia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Eatopia.Infrastructure.Services;

public class MealService
{
    private readonly EatopiaDbContext _context;
    private readonly IFoodAiClient _aiClient;

    public MealService(EatopiaDbContext context, IFoodAiClient aiClient)
    {
        _context = context;
        _aiClient = aiClient;
    }

    public async Task<MealLog> CreateMealAsync(Guid userId, CreateMealDto dto)
    {
        if (dto.QuantityGrams <= 0)
            throw new ApiException("QuantityGrams must be > 0", 400, "VALIDATION_ERROR");

        var food = await _context.FoodItems.FirstOrDefaultAsync(x => x.Id == dto.FoodId);

        if (food == null)
            throw new ApiException("Food item not found", 404, "NOT_FOUND");

        var (calories, protein, fat, carbs) = CalculateMacros(food, dto.QuantityGrams);

        var meal = new MealLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FoodId = food.Id,
            MealImageUrl = dto.MealImageUrl,
            DetectedAt = DateTime.UtcNow,
            QuantityGrams = dto.QuantityGrams,
            CalculatedCalories = calories,
            CalculatedProtein = protein,
            CalculatedFat = fat,
            CalculatedCarbs = carbs,
            CreatedAt = DateTime.UtcNow
        };

        _context.MealLogs.Add(meal);
        await _context.SaveChangesAsync();

        return meal;
    }

    public async Task<object> CreateFrontendMealAsync(Guid userId, FrontendMealDto dto)
    {
        var food = await GetOrCreateFrontendFoodAsync(dto);
        var meal = BuildMealLog(userId, food, dto.QuantityGrams, dto.MealImageUrl);

        _context.MealLogs.Add(meal);
        await _context.SaveChangesAsync();

        return ToFrontendMeal(meal, food);
    }

    public async Task<object> UpdateFrontendMealAsync(Guid userId, Guid mealId, FrontendMealDto dto)
    {
        var meal = await _context.MealLogs
            .Include(x => x.FoodItem)
            .FirstOrDefaultAsync(x => x.Id == mealId && x.UserId == userId);

        if (meal == null)
            throw new ApiException("Meal log not found", 404, "NOT_FOUND");

        var food = await GetOrCreateFrontendFoodAsync(dto);
        var (calories, protein, fat, carbs) = CalculateMacros(food, dto.QuantityGrams);

        meal.FoodId = food.Id;
        meal.FoodItem = food;
        meal.MealImageUrl = dto.MealImageUrl;
        meal.DetectedAt = DateTime.UtcNow;
        meal.QuantityGrams = dto.QuantityGrams;
        meal.CalculatedCalories = calories;
        meal.CalculatedProtein = protein;
        meal.CalculatedFat = fat;
        meal.CalculatedCarbs = carbs;

        await _context.SaveChangesAsync();

        return ToFrontendMeal(meal, food);
    }

    public async Task DeleteFrontendMealAsync(Guid userId, Guid mealId)
    {
        var meal = await _context.MealLogs.FirstOrDefaultAsync(x => x.Id == mealId && x.UserId == userId);
        if (meal == null)
            throw new ApiException("Meal log not found", 404, "NOT_FOUND");

        _context.MealLogs.Remove(meal);
        await _context.SaveChangesAsync();
    }

    public async Task<MealLog> AnalyzeAndSaveAsync(Guid userId, AnalyzeMealDto dto)
    {
        if (dto.QuantityGrams <= 0)
            throw new ApiException("QuantityGrams must be > 0", 400, "VALIDATION_ERROR");

        var ai = await _aiClient.AnalyzeFoodImageAsync(dto.ImageUrl);

        var foodName = ai.FoodName.Trim();

        var food = await _context.FoodItems
            .FirstOrDefaultAsync(x => x.Name.ToLower() == foodName.ToLower());

        if (food == null)
            throw new ApiException($"Food item '{foodName}' not found in database", 404, "FOOD_NOT_FOUND");

        var (calories, protein, fat, carbs) = CalculateMacros(food, dto.QuantityGrams);

        var meal = new MealLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FoodId = food.Id,
            MealImageUrl = dto.ImageUrl,
            DetectedAt = DateTime.UtcNow,
            QuantityGrams = dto.QuantityGrams,
            CalculatedCalories = calories,
            CalculatedProtein = protein,
            CalculatedFat = fat,
            CalculatedCarbs = carbs,
            CreatedAt = DateTime.UtcNow
        };

        _context.MealLogs.Add(meal);
        await _context.SaveChangesAsync();

        return meal;
    }

    public async Task<PagedResult<object>> GetHistoryAsync(Guid userId, DateTime? from, DateTime? to, int pageIndex, int pageSize)
    {
        var query = _context.MealLogs
            .Include(x => x.FoodItem)
            .Where(x => x.UserId == userId);

        if (from.HasValue)
            query = query.Where(x => x.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(x => x.CreatedAt <= to.Value);

        var paged = await query
            .OrderByDescending(x => x.CreatedAt)
            .ToPagedResultAsync(pageIndex, pageSize);

        return new PagedResult<object>
        {
            PageIndex = paged.PageIndex,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount,
            Items = paged.Items.Select(m => ToFrontendMeal(m, m.FoodItem)).Cast<object>().ToList()
        };
    }

    private async Task<FoodItem> GetOrCreateFrontendFoodAsync(FrontendMealDto dto)
    {
        var name = dto.MealName.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ApiException("Meal name is required", 400, "VALIDATION_ERROR");

        if (dto.QuantityGrams <= 0)
            throw new ApiException("QuantityGrams must be > 0", 400, "VALIDATION_ERROR");

        var food = await _context.FoodItems.FirstOrDefaultAsync(x => x.Name.ToLower() == name.ToLower());
        if (food != null)
        {
            food.CaloriesPer100g = ResolveCaloriesPer100g(dto);
            food.ProteinPer100g = dto.ProteinPer100g ?? food.ProteinPer100g;
            food.FatPer100g = dto.FatPer100g ?? food.FatPer100g;
            food.CarbsPer100g = dto.CarbsPer100g ?? food.CarbsPer100g;
            food.ServingSize = string.IsNullOrWhiteSpace(dto.ServingSize) ? food.ServingSize : dto.ServingSize;
            return food;
        }

        food = new FoodItem
        {
            Id = Guid.NewGuid(),
            Name = name,
            CaloriesPer100g = ResolveCaloriesPer100g(dto),
            ProteinPer100g = dto.ProteinPer100g ?? 0,
            FatPer100g = dto.FatPer100g ?? 0,
            CarbsPer100g = dto.CarbsPer100g ?? 0,
            ServingSize = string.IsNullOrWhiteSpace(dto.ServingSize) ? "1 serving" : dto.ServingSize,
            CreatedAt = DateTime.UtcNow
        };

        _context.FoodItems.Add(food);
        return food;
    }

    private static decimal ResolveCaloriesPer100g(FrontendMealDto dto)
    {
        if (dto.CaloriesPer100g.HasValue)
            return dto.CaloriesPer100g.Value;

        if (dto.QuantityGrams <= 0)
            return dto.CaloriesPerServing;

        return Math.Round((dto.CaloriesPerServing * 100) / dto.QuantityGrams, 2);
    }

    private static MealLog BuildMealLog(Guid userId, FoodItem food, decimal quantityGrams, string? imageUrl)
    {
        var (calories, protein, fat, carbs) = CalculateMacros(food, quantityGrams);

        return new MealLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FoodId = food.Id,
            FoodItem = food,
            MealImageUrl = imageUrl,
            DetectedAt = DateTime.UtcNow,
            QuantityGrams = quantityGrams,
            CalculatedCalories = calories,
            CalculatedProtein = protein,
            CalculatedFat = fat,
            CalculatedCarbs = carbs,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static object ToFrontendMeal(MealLog m, FoodItem food) => new
    {
        id = m.Id,
        mealName = food.Name,
        mealImageUrl = m.MealImageUrl,
        quantityGrams = m.QuantityGrams,
        calculatedCalories = m.CalculatedCalories,
        calculatedProtein = m.CalculatedProtein,
        calculatedFat = m.CalculatedFat,
        calculatedCarbs = m.CalculatedCarbs,
        createdAt = m.CreatedAt,
        detectedAt = m.DetectedAt,
        food = new
        {
            id = food.Id,
            name = food.Name,
            caloriesPer100g = food.CaloriesPer100g,
            proteinPer100g = food.ProteinPer100g,
            fatPer100g = food.FatPer100g,
            carbsPer100g = food.CarbsPer100g,
            servingSize = food.ServingSize
        }
    };

    private static (decimal calories, decimal protein, decimal fat, decimal carbs) CalculateMacros(FoodItem food, decimal grams)
    {
        var calories = Math.Round((food.CaloriesPer100g * grams) / 100, 2);
        var protein = Math.Round((food.ProteinPer100g * grams) / 100, 2);
        var fat = Math.Round((food.FatPer100g * grams) / 100, 2);
        var carbs = Math.Round((food.CarbsPer100g * grams) / 100, 2);
        return (calories, protein, fat, carbs);
    }
}

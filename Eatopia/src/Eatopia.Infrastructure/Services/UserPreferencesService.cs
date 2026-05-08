using Eatopia.Application.DTOs.Users;
using Eatopia.Application.Exceptions;
using Eatopia.Domain.Entities;
using Eatopia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Eatopia.Infrastructure.Services;

public class UserPreferencesService
{
    private readonly EatopiaDbContext _context;

    public UserPreferencesService(EatopiaDbContext context)
    {
        _context = context;
    }

    // Allergies
    public async Task<List<UserAllergy>> GetAllergiesAsync(Guid userId)
    {
        return await _context.UserAllergies
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.AllergyName)
            .ToListAsync();
    }

    public async Task<UserAllergy> AddAllergyAsync(Guid userId, AddAllergyDto dto)
    {
        var allergyName = dto.AllergyName.Trim();

        var exists = await _context.UserAllergies
            .AnyAsync(x => x.UserId == userId && x.AllergyName == allergyName);

        if (exists)
            throw new ApiException("Allergy already exists", 409, "ALREADY_EXISTS");

        var allergy = new UserAllergy
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AllergyName = allergyName
        };

        _context.UserAllergies.Add(allergy);
        await _context.SaveChangesAsync();

        return allergy;
    }

    public async Task RemoveAllergyAsync(Guid userId, Guid allergyId)
    {
        var allergy = await _context.UserAllergies.FirstOrDefaultAsync(x => x.Id == allergyId && x.UserId == userId);
        if (allergy == null)
            throw new ApiException("Allergy not found", 404, "NOT_FOUND");

        _context.UserAllergies.Remove(allergy);
        await _context.SaveChangesAsync();
    }

    // Disliked Foods
    public async Task<List<UserDislikedFood>> GetDislikedFoodsAsync(Guid userId)
    {
        return await _context.UserDislikedFoods
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.FoodName)
            .ToListAsync();
    }

    public async Task<UserDislikedFood> AddDislikedFoodAsync(Guid userId, AddDislikedFoodDto dto)
    {
        var foodName = dto.FoodName.Trim();

        var exists = await _context.UserDislikedFoods
            .AnyAsync(x => x.UserId == userId && x.FoodName == foodName);

        if (exists)
            throw new ApiException("Disliked food already exists", 409, "ALREADY_EXISTS");

        var item = new UserDislikedFood
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FoodName = foodName
        };

        _context.UserDislikedFoods.Add(item);
        await _context.SaveChangesAsync();

        return item;
    }

    public async Task RemoveDislikedFoodAsync(Guid userId, Guid dislikedFoodId)
    {
        var item = await _context.UserDislikedFoods.FirstOrDefaultAsync(x => x.Id == dislikedFoodId && x.UserId == userId);
        if (item == null)
            throw new ApiException("Disliked food not found", 404, "NOT_FOUND");

        _context.UserDislikedFoods.Remove(item);
        await _context.SaveChangesAsync();
    }
}

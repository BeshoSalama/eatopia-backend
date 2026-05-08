using Eatopia.Application.Common;
using Eatopia.Application.DTOs.Food;
using Eatopia.Application.Exceptions;
using Eatopia.Domain.Entities;
using Eatopia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Eatopia.Infrastructure.Services;

public class FoodService
{
    private readonly EatopiaDbContext _context;

    public FoodService(EatopiaDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<FoodItem>> GetAllAsync(string? search, int pageIndex, int pageSize)
    {
        var query = _context.FoodItems.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(x => x.Name.Contains(search));
        }

        return await query
            .OrderBy(x => x.Name)
            .ToPagedResultAsync(pageIndex, pageSize);
    }

    public async Task<FoodItem> GetByIdAsync(Guid id)
    {
        var food = await _context.FoodItems.FindAsync(id);
        if (food == null)
            throw new ApiException("Food not found", 404, "NOT_FOUND");

        return food;
    }

    public async Task<FoodItem> CreateAsync(CreateFoodItemDto dto)
    {
        var name = dto.Name.Trim();

        var exists = await _context.FoodItems.AnyAsync(x => x.Name == name);
        if (exists)
            throw new ApiException("Food item already exists", 409, "ALREADY_EXISTS");

        var food = new FoodItem
        {
            Id = Guid.NewGuid(),
            Name = name,
            CaloriesPer100g = dto.CaloriesPer100g,
            ProteinPer100g = dto.ProteinPer100g,
            FatPer100g = dto.FatPer100g,
            CarbsPer100g = dto.CarbsPer100g,
            ServingSize = dto.ServingSize
        };

        _context.FoodItems.Add(food);
        await _context.SaveChangesAsync();

        return food;
    }
}

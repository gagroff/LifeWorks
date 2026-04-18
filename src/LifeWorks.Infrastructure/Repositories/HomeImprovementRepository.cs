using LifeWorks.Application.Models;
using LifeWorks.Application.Repositories;
using LifeWorks.Domain.Entities;
using LifeWorks.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LifeWorks.Infrastructure.Repositories;

public class HomeImprovementRepository(AppDbContext context) : RepositoryBase<HomeImprovement>(context), IHomeImprovementRepository
{
    public async Task<List<HomeImprovement>> GetFilteredAsync(HomeImprovementFilter filter)
    {
        var query = ApplyFilter(Context.HomeImprovements
            .Include(h => h.Property)
            .Include(h => h.Category)
            .Include(h => h.Contractor), filter);

        return await query.OrderByDescending(h => h.DateCompleted).ToListAsync();
    }

    public async Task<HomeImprovement?> GetWithDetailsAsync(Guid id) =>
        await Context.HomeImprovements
            .Include(h => h.Property)
            .Include(h => h.Category)
            .Include(h => h.Contractor)
            .FirstOrDefaultAsync(h => h.Id == id);

    public async Task<decimal> GetTotalCostAsync(HomeImprovementFilter filter)
    {
        var query = ApplyFilter(Context.HomeImprovements, filter);
        return await query.SumAsync(h => h.Cost ?? 0);
    }

    private static IQueryable<HomeImprovement> ApplyFilter(IQueryable<HomeImprovement> query, HomeImprovementFilter filter)
    {
        if (filter.PropertyId.HasValue)
            query = query.Where(h => h.PropertyId == filter.PropertyId);

        if (filter.CategoryId.HasValue)
            query = query.Where(h => h.CategoryId == filter.CategoryId);

        if (filter.DateFrom.HasValue)
            query = query.Where(h => h.DateCompleted >= filter.DateFrom);

        if (filter.DateTo.HasValue)
            query = query.Where(h => h.DateCompleted <= filter.DateTo);

        return query;
    }
}

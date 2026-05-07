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
            .Include(h => h.Contractor)
            .Include(h => h.Attachments), filter);

        return await query.OrderByDescending(h => h.DateCompleted).ToListAsync();
    }

    public async Task<HomeImprovement?> GetWithDetailsAsync(Guid id) =>
        await Context.HomeImprovements
            .Include(h => h.Property)
            .Include(h => h.Category)
            .Include(h => h.Contractor)
            .Include(h => h.Attachments)
            .FirstOrDefaultAsync(h => h.Id == id);

    public async Task<decimal> GetTotalCostAsync(HomeImprovementFilter filter)
    {
        var query = ApplyFilter(Context.HomeImprovements, filter);
        return await query.SumAsync(h => h.Cost ?? 0);
    }

    public async Task<List<(string PropertyName, decimal TotalCost)>> GetCostByPropertyAsync() =>
        await Context.HomeImprovements
            .GroupBy(h => new { h.PropertyId, h.Property.Name })
            .Select(g => new { g.Key.Name, TotalCost = g.Sum(h => h.Cost ?? 0) })
            .OrderByDescending(x => x.TotalCost)
            .Select(x => ValueTuple.Create(x.Name, x.TotalCost))
            .ToListAsync();

    public async Task<List<(string CategoryName, decimal TotalCost)>> GetCostByCategoryAsync() =>
        await Context.HomeImprovements
            .GroupBy(h => new { h.CategoryId, h.Category.Name })
            .Select(g => new { g.Key.Name, TotalCost = g.Sum(h => h.Cost ?? 0) })
            .OrderByDescending(x => x.TotalCost)
            .Select(x => ValueTuple.Create(x.Name, x.TotalCost))
            .ToListAsync();

    public async Task<List<HomeImprovement>> GetExpiringWarrantiesAsync(int withinDays)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var cutoff = today.AddDays(withinDays);
        return await Context.HomeImprovements
            .Include(h => h.Property)
            .Include(h => h.Category)
            .Where(h => h.WarrantyExpiration.HasValue && h.WarrantyExpiration.Value <= cutoff)
            .OrderBy(h => h.WarrantyExpiration)
            .ToListAsync();
    }

    public async Task<List<HomeImprovement>> GetRecentAsync(int count) =>
        await Context.HomeImprovements
            .Include(h => h.Property)
            .Include(h => h.Category)
            .OrderByDescending(h => h.DateCompleted)
            .Take(count)
            .ToListAsync();

    private static IQueryable<HomeImprovement> ApplyFilter(IQueryable<HomeImprovement> query, HomeImprovementFilter filter)
    {
        if (filter.PropertyId.HasValue)
            query = query.Where(h => h.PropertyId == filter.PropertyId);

        if (filter.CategoryId.HasValue)
            query = query.Where(h => h.CategoryId == filter.CategoryId);

        if (filter.ContractorId.HasValue)
            query = query.Where(h => h.ContractorId == filter.ContractorId);

        if (filter.DateFrom.HasValue)
            query = query.Where(h => h.DateCompleted >= filter.DateFrom);

        if (filter.DateTo.HasValue)
            query = query.Where(h => h.DateCompleted <= filter.DateTo);

        return query;
    }
}

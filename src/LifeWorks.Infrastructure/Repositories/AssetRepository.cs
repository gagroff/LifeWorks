using LifeWorks.Application.Repositories;
using LifeWorks.Domain.Entities;
using LifeWorks.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LifeWorks.Infrastructure.Repositories;

public class AssetRepository(AppDbContext context) : RepositoryBase<Asset>(context), IAssetRepository
{
    public new async Task<List<Asset>> GetAllAsync() =>
        await Context.Assets
            .Include(a => a.Property)
            .OrderBy(a => a.Name)
            .ToListAsync();

    public new async Task<Asset?> GetByIdAsync(Guid id) =>
        await Context.Assets
            .Include(a => a.Property)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<List<Asset>> GetByPropertyAsync(Guid propertyId) =>
        await Context.Assets
            .Include(a => a.Property)
            .Where(a => a.PropertyId == propertyId)
            .OrderBy(a => a.Name)
            .ToListAsync();

    public async Task<List<Asset>> GetExpiringWarrantiesAsync(int withinDays)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var cutoff = today.AddDays(withinDays);

        return await Context.Assets
            .Include(a => a.Property)
            .Where(a => a.WarrantyExpiration.HasValue && a.WarrantyExpiration.Value <= cutoff)
            .OrderBy(a => a.WarrantyExpiration)
            .ThenBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<List<string>> GetDistinctCategoriesAsync() =>
        await Context.Assets
            .Where(a => !string.IsNullOrWhiteSpace(a.Category))
            .Select(a => a.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
}

using LifeWorks.Application.Repositories;
using LifeWorks.Domain.Entities;

namespace LifeWorks.Application.Services;

public class AssetService(IAssetRepository repository) : IAssetService
{
    public Task<List<Asset>> GetAllAsync() =>
        repository.GetAllAsync();

    public Task<List<Asset>> GetAllByPropertyAsync(Guid propertyId) =>
        repository.GetByPropertyAsync(propertyId);

    public Task<Asset?> GetByIdAsync(Guid id) =>
        repository.GetByIdAsync(id);

    public async Task AddAsync(Asset asset)
    {
        asset.Id = Guid.NewGuid();
        asset.CreatedAt = DateTime.UtcNow;
        asset.UpdatedAt = DateTime.UtcNow;
        await repository.AddAsync(asset);
        await repository.SaveChangesAsync();
    }

    public async Task UpdateAsync(Asset asset)
    {
        asset.UpdatedAt = DateTime.UtcNow;
        await repository.UpdateAsync(asset);
        await repository.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var asset = await repository.GetByIdAsync(id);
        if (asset is null)
            return;

        await repository.DeleteAsync(asset);
        await repository.SaveChangesAsync();
    }

    public Task<List<Asset>> GetExpiringWarrantiesAsync(int withinDays) =>
        repository.GetExpiringWarrantiesAsync(withinDays);

    public Task<List<string>> GetDistinctCategoriesAsync() =>
        repository.GetDistinctCategoriesAsync();
}

using LifeWorks.Domain.Entities;

namespace LifeWorks.Application.Services;

public interface IAssetService
{
    Task<List<Asset>> GetAllAsync();
    Task<List<Asset>> GetAllByPropertyAsync(Guid propertyId);
    Task<Asset?> GetByIdAsync(Guid id);
    Task AddAsync(Asset asset);
    Task UpdateAsync(Asset asset);
    Task DeleteAsync(Guid id);
    Task<List<Asset>> GetExpiringWarrantiesAsync(int withinDays);
    Task<List<string>> GetDistinctCategoriesAsync();
}

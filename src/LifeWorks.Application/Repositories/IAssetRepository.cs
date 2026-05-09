using LifeWorks.Domain.Entities;

namespace LifeWorks.Application.Repositories;

public interface IAssetRepository : IRepository<Asset>
{
    Task<List<Asset>> GetByPropertyAsync(Guid propertyId);
    Task<List<Asset>> GetExpiringWarrantiesAsync(int withinDays);
    Task<List<string>> GetDistinctCategoriesAsync();
}

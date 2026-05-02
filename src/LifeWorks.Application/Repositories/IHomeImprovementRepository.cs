using LifeWorks.Application.Models;
using LifeWorks.Domain.Entities;

namespace LifeWorks.Application.Repositories;

public interface IHomeImprovementRepository : IRepository<HomeImprovement>
{
    Task<List<HomeImprovement>> GetFilteredAsync(HomeImprovementFilter filter);
    Task<HomeImprovement?> GetWithDetailsAsync(Guid id);
    Task<decimal> GetTotalCostAsync(HomeImprovementFilter filter);
    Task<List<(string PropertyName, decimal TotalCost)>> GetCostByPropertyAsync();
    Task<List<(string CategoryName, decimal TotalCost)>> GetCostByCategoryAsync();
    Task<List<HomeImprovement>> GetExpiringWarrantiesAsync(int withinDays);
    Task<List<HomeImprovement>> GetRecentAsync(int count);
}

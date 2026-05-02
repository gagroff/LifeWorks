using LifeWorks.Application.Models;
using LifeWorks.Domain.Entities;

namespace LifeWorks.Application.Services;

public interface IHomeImprovementService
{
    Task<List<HomeImprovement>> GetAllAsync(HomeImprovementFilter? filter = null);
    Task<HomeImprovement?> GetByIdAsync(Guid id);
    Task AddAsync(HomeImprovement improvement);
    Task UpdateAsync(HomeImprovement improvement);
    Task DeleteAsync(Guid id);
    Task<decimal> GetTotalCostAsync(HomeImprovementFilter? filter = null);
    Task<List<(string PropertyName, decimal TotalCost)>> GetCostByPropertyAsync();
    Task<List<(string CategoryName, decimal TotalCost)>> GetCostByCategoryAsync();
    Task<List<HomeImprovement>> GetExpiringWarrantiesAsync(int withinDays);
    Task<List<HomeImprovement>> GetRecentAsync(int count);
}

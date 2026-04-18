using LifeWorks.Application.Models;
using LifeWorks.Domain.Entities;

namespace LifeWorks.Application.Repositories;

public interface IHomeImprovementRepository : IRepository<HomeImprovement>
{
    Task<List<HomeImprovement>> GetFilteredAsync(HomeImprovementFilter filter);
    Task<HomeImprovement?> GetWithDetailsAsync(Guid id);
    Task<decimal> GetTotalCostAsync(HomeImprovementFilter filter);
}

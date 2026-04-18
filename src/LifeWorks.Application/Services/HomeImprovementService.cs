using LifeWorks.Application.Models;
using LifeWorks.Application.Repositories;
using LifeWorks.Domain.Entities;

namespace LifeWorks.Application.Services;

public class HomeImprovementService(IHomeImprovementRepository repository) : IHomeImprovementService
{
    public Task<List<HomeImprovement>> GetAllAsync(HomeImprovementFilter? filter = null) =>
        repository.GetFilteredAsync(filter ?? new HomeImprovementFilter());

    public Task<HomeImprovement?> GetByIdAsync(Guid id) =>
        repository.GetWithDetailsAsync(id);

    public async Task AddAsync(HomeImprovement improvement)
    {
        improvement.Id = Guid.NewGuid();
        improvement.CreatedAt = DateTime.UtcNow;
        improvement.UpdatedAt = DateTime.UtcNow;
        await repository.AddAsync(improvement);
        await repository.SaveChangesAsync();
    }

    public async Task UpdateAsync(HomeImprovement improvement)
    {
        improvement.UpdatedAt = DateTime.UtcNow;
        await repository.UpdateAsync(improvement);
        await repository.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var improvement = await repository.GetByIdAsync(id);
        if (improvement is null)
            return;

        await repository.DeleteAsync(improvement);
        await repository.SaveChangesAsync();
    }

    public Task<decimal> GetTotalCostAsync(HomeImprovementFilter? filter = null) =>
        repository.GetTotalCostAsync(filter ?? new HomeImprovementFilter());
}

using LifeWorks.Application.Repositories;
using LifeWorks.Domain.Entities;

namespace LifeWorks.Application.Services;

public class ContractorService(IContractorRepository repository) : IContractorService
{
    public Task<List<Contractor>> GetAllAsync(string? searchTerm = null) =>
        repository.SearchAsync(searchTerm);

    public Task<Contractor?> GetByIdAsync(Guid id) =>
        repository.GetByIdAsync(id);

    public Task<int> GetLinkedImprovementCountAsync(Guid contractorId) =>
        repository.GetLinkedImprovementCountAsync(contractorId);

    public async Task AddAsync(Contractor contractor)
    {
        contractor.Id = Guid.NewGuid();
        contractor.CreatedAt = DateTime.UtcNow;
        contractor.UpdatedAt = DateTime.UtcNow;
        await repository.AddAsync(contractor);
        await repository.SaveChangesAsync();
    }

    public async Task UpdateAsync(Contractor contractor)
    {
        contractor.UpdatedAt = DateTime.UtcNow;
        await repository.UpdateAsync(contractor);
        await repository.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var count = await repository.GetLinkedImprovementCountAsync(id);
        if (count > 0)
            return false;

        var contractor = await repository.GetByIdAsync(id);
        if (contractor is null)
            return false;

        await repository.DeleteAsync(contractor);
        await repository.SaveChangesAsync();
        return true;
    }
}

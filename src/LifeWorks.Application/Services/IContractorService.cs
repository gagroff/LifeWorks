using LifeWorks.Domain.Entities;

namespace LifeWorks.Application.Services;

public interface IContractorService
{
    Task<List<Contractor>> GetAllAsync(string? searchTerm = null);
    Task<Contractor?> GetByIdAsync(Guid id);
    Task<int> GetLinkedImprovementCountAsync(Guid contractorId);
    Task AddAsync(Contractor contractor);
    Task UpdateAsync(Contractor contractor);
    Task<bool> DeleteAsync(Guid id);
}

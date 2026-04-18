using LifeWorks.Domain.Entities;

namespace LifeWorks.Application.Repositories;

public interface IContractorRepository : IRepository<Contractor>
{
    Task<List<Contractor>> SearchAsync(string? searchTerm);
    Task<int> GetLinkedImprovementCountAsync(Guid contractorId);
}

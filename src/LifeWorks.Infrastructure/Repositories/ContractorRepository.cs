using LifeWorks.Application.Repositories;
using LifeWorks.Domain.Entities;
using LifeWorks.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LifeWorks.Infrastructure.Repositories;

public class ContractorRepository(AppDbContext context) : RepositoryBase<Contractor>(context), IContractorRepository
{
    public async Task<List<Contractor>> SearchAsync(string? searchTerm)
    {
        var query = Context.Contractors.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(c => c.Name.Contains(searchTerm) || (c.CompanyName != null && c.CompanyName.Contains(searchTerm)));

        return await query.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<int> GetLinkedImprovementCountAsync(Guid contractorId) =>
        await Context.HomeImprovements.CountAsync(h => h.ContractorId == contractorId);
}

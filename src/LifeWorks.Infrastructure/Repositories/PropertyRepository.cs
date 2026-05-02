using LifeWorks.Application.Repositories;
using LifeWorks.Domain.Entities;
using LifeWorks.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LifeWorks.Infrastructure.Repositories;

public class PropertyRepository(AppDbContext context) : RepositoryBase<Property>(context), IPropertyRepository
{
    public Task<bool> HasImprovementsAsync(Guid propertyId) =>
        Context.HomeImprovements.AnyAsync(h => h.PropertyId == propertyId);
}

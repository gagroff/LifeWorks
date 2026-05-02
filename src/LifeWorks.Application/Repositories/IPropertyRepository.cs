using LifeWorks.Domain.Entities;

namespace LifeWorks.Application.Repositories;

public interface IPropertyRepository : IRepository<Property>
{
    Task<bool> HasImprovementsAsync(Guid propertyId);
}

using LifeWorks.Application.Repositories;
using LifeWorks.Domain.Entities;
using LifeWorks.Infrastructure.Data;

namespace LifeWorks.Infrastructure.Repositories;

public class PropertyRepository(AppDbContext context) : RepositoryBase<Property>(context), IPropertyRepository
{
}

using LifeWorks.Application.Repositories;
using LifeWorks.Domain.Entities;

namespace LifeWorks.Application.Services;

public class PropertyService(IPropertyRepository repository) : IPropertyService
{
    public Task<List<Property>> GetAllAsync() =>
        repository.GetAllAsync();
}

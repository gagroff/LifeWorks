using LifeWorks.Domain.Entities;

namespace LifeWorks.Application.Services;

public interface IPropertyService
{
    Task<List<Property>> GetAllAsync();
}

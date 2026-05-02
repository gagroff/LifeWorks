using LifeWorks.Domain.Entities;

namespace LifeWorks.Application.Services;

public interface IPropertyService
{
    Task<List<Property>> GetAllAsync();
    Task<Property?> GetByIdAsync(Guid id);
    Task AddAsync(Property entity);
    Task UpdateAsync(Property entity);
    Task<bool> DeleteAsync(Guid id);
}

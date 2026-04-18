using LifeWorks.Domain.Entities;

namespace LifeWorks.Application.Services;

public interface ICategoryService
{
    Task<List<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(Guid id);
    Task AddAsync(Category category);
    Task UpdateAsync(Category category);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> CanDeleteAsync(Guid id);
}

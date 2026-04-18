using LifeWorks.Domain.Entities;

namespace LifeWorks.Application.Repositories;

public interface ICategoryRepository : IRepository<Category>
{
    Task<List<Category>> GetAllOrderedAsync();
    Task<bool> HasLinkedImprovementsAsync(Guid categoryId);
}

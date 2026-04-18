using LifeWorks.Application.Repositories;
using LifeWorks.Domain.Entities;
using LifeWorks.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LifeWorks.Infrastructure.Repositories;

public class CategoryRepository(AppDbContext context) : RepositoryBase<Category>(context), ICategoryRepository
{
    public async Task<List<Category>> GetAllOrderedAsync() =>
        await Context.Categories
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();

    public async Task<bool> HasLinkedImprovementsAsync(Guid categoryId) =>
        await Context.HomeImprovements.AnyAsync(h => h.CategoryId == categoryId);
}

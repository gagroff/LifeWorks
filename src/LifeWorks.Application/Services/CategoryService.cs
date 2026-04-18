using LifeWorks.Application.Repositories;
using LifeWorks.Domain.Entities;

namespace LifeWorks.Application.Services;

public class CategoryService(ICategoryRepository repository) : ICategoryService
{
    public Task<List<Category>> GetAllAsync() =>
        repository.GetAllOrderedAsync();

    public Task<Category?> GetByIdAsync(Guid id) =>
        repository.GetByIdAsync(id);

    public async Task AddAsync(Category category)
    {
        category.Id = Guid.NewGuid();
        category.IsSeeded = false;
        await repository.AddAsync(category);
        await repository.SaveChangesAsync();
    }

    public async Task UpdateAsync(Category category)
    {
        await repository.UpdateAsync(category);
        await repository.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var category = await repository.GetByIdAsync(id);
        if (category is null || category.IsSeeded)
            return false;

        if (await repository.HasLinkedImprovementsAsync(id))
            return false;

        await repository.DeleteAsync(category);
        await repository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CanDeleteAsync(Guid id)
    {
        var category = await repository.GetByIdAsync(id);
        if (category is null || category.IsSeeded)
            return false;

        return !await repository.HasLinkedImprovementsAsync(id);
    }
}

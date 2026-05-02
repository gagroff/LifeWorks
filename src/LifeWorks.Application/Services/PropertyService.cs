using LifeWorks.Application.Repositories;
using LifeWorks.Domain.Entities;

namespace LifeWorks.Application.Services;

public class PropertyService(IPropertyRepository repository) : IPropertyService
{
    public Task<List<Property>> GetAllAsync() =>
        repository.GetAllAsync();

    public Task<Property?> GetByIdAsync(Guid id) =>
        repository.GetByIdAsync(id);

    public async Task AddAsync(Property entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await repository.AddAsync(entity);
        await repository.SaveChangesAsync();
    }

    public async Task UpdateAsync(Property entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        await repository.UpdateAsync(entity);
        await repository.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        if (await repository.HasImprovementsAsync(id))
            return false;

        var property = await repository.GetByIdAsync(id);
        if (property is null)
            return false;

        await repository.DeleteAsync(property);
        await repository.SaveChangesAsync();
        return true;
    }
}

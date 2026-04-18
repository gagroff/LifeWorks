using LifeWorks.Application.Repositories;
using LifeWorks.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LifeWorks.Infrastructure.Repositories;

public abstract class RepositoryBase<T>(AppDbContext context) : IRepository<T> where T : class
{
    protected AppDbContext Context { get; } = context;

    public async Task<List<T>> GetAllAsync() =>
        await Context.Set<T>().ToListAsync();

    public async Task<T?> GetByIdAsync(Guid id) =>
        await Context.Set<T>().FindAsync(id);

    public async Task AddAsync(T entity) =>
        await Context.Set<T>().AddAsync(entity);

    public Task UpdateAsync(T entity)
    {
        Context.Set<T>().Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(T entity)
    {
        Context.Set<T>().Remove(entity);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() =>
        await Context.SaveChangesAsync();
}

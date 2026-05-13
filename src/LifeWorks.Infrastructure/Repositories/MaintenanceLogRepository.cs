using LifeWorks.Application.Repositories;
using LifeWorks.Domain.Entities;
using LifeWorks.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LifeWorks.Infrastructure.Repositories;

public class MaintenanceLogRepository(AppDbContext context) : RepositoryBase<MaintenanceLog>(context), IMaintenanceLogRepository
{
    public async Task<List<MaintenanceLog>> GetByTaskAsync(Guid taskId) =>
        await Context.MaintenanceLogs
            .Where(l => l.MaintenanceTaskId == taskId)
            .OrderByDescending(l => l.CompletedDate)
            .ToListAsync();
}

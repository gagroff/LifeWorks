using LifeWorks.Domain.Entities;

namespace LifeWorks.Application.Repositories;

public interface IMaintenanceLogRepository : IRepository<MaintenanceLog>
{
    Task<List<MaintenanceLog>> GetByTaskAsync(Guid taskId);
}

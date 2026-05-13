using LifeWorks.Domain.Entities;

namespace LifeWorks.Application.Repositories;

public interface IMaintenanceTaskRepository : IRepository<MaintenanceTask>
{
    Task<List<MaintenanceTask>> GetByPropertyAsync(Guid? propertyId);
    Task<List<MaintenanceTask>> GetOverdueOrUpcomingAsync(int withinDays);
    Task<MaintenanceTask?> GetWithLogsAsync(Guid id);
}

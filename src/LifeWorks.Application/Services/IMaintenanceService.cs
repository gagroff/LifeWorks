using LifeWorks.Domain.Entities;

namespace LifeWorks.Application.Services;

public interface IMaintenanceService
{
    Task<List<MaintenanceTask>> GetAllTasksAsync(Guid? propertyId = null);
    Task<MaintenanceTask?> GetTaskByIdAsync(Guid id);
    Task<MaintenanceTask?> GetTaskWithLogsAsync(Guid id);
    Task AddTaskAsync(MaintenanceTask task);
    Task UpdateTaskAsync(MaintenanceTask task);
    Task DeleteTaskAsync(Guid id);
    Task<List<MaintenanceTask>> GetOverdueOrUpcomingAsync(int withinDays = 30);
    Task LogCompletionAsync(Guid taskId, DateOnly completedDate, decimal? cost, string? notes);
    Task DeleteLogAsync(Guid logId);
}

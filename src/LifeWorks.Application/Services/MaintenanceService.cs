using LifeWorks.Application.Repositories;
using LifeWorks.Domain.Entities;

namespace LifeWorks.Application.Services;

public class MaintenanceService(
    IMaintenanceTaskRepository taskRepository,
    IMaintenanceLogRepository logRepository) : IMaintenanceService
{
    public Task<List<MaintenanceTask>> GetAllTasksAsync(Guid? propertyId = null) =>
        taskRepository.GetByPropertyAsync(propertyId);

    public Task<MaintenanceTask?> GetTaskByIdAsync(Guid id) =>
        taskRepository.GetByIdAsync(id);

    public Task<MaintenanceTask?> GetTaskWithLogsAsync(Guid id) =>
        taskRepository.GetWithLogsAsync(id);

    public async Task AddTaskAsync(MaintenanceTask task)
    {
        task.Id = Guid.NewGuid();
        task.CreatedAt = DateTime.UtcNow;
        task.UpdatedAt = DateTime.UtcNow;
        await taskRepository.AddAsync(task);
        await taskRepository.SaveChangesAsync();
    }

    public async Task UpdateTaskAsync(MaintenanceTask task)
    {
        task.UpdatedAt = DateTime.UtcNow;
        await taskRepository.UpdateAsync(task);
        await taskRepository.SaveChangesAsync();
    }

    public async Task DeleteTaskAsync(Guid id)
    {
        var task = await taskRepository.GetByIdAsync(id);
        if (task is null)
            return;

        await taskRepository.DeleteAsync(task);
        await taskRepository.SaveChangesAsync();
    }

    public Task<List<MaintenanceTask>> GetOverdueOrUpcomingAsync(int withinDays = 30) =>
        taskRepository.GetOverdueOrUpcomingAsync(withinDays);

    public async Task LogCompletionAsync(Guid taskId, DateOnly completedDate, decimal? cost, string? notes)
    {
        var task = await taskRepository.GetByIdAsync(taskId);
        if (task is null)
            return;

        var log = new MaintenanceLog
        {
            Id = Guid.NewGuid(),
            MaintenanceTaskId = taskId,
            CompletedDate = completedDate,
            Cost = cost,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };
        await logRepository.AddAsync(log);

        task.LastCompletedDate = completedDate;
        task.UpdatedAt = DateTime.UtcNow;
        await taskRepository.UpdateAsync(task);

        await taskRepository.SaveChangesAsync();
    }

    public async Task DeleteLogAsync(Guid logId)
    {
        var log = await logRepository.GetByIdAsync(logId);
        if (log is null)
            return;

        await logRepository.DeleteAsync(log);
        await logRepository.SaveChangesAsync();
    }
}

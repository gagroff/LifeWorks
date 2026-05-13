using LifeWorks.Application.Repositories;
using LifeWorks.Domain.Entities;
using LifeWorks.Domain.Enums;
using LifeWorks.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LifeWorks.Infrastructure.Repositories;

public class MaintenanceTaskRepository(AppDbContext context) : RepositoryBase<MaintenanceTask>(context), IMaintenanceTaskRepository
{
    public new async Task<List<MaintenanceTask>> GetAllAsync() =>
        await Context.MaintenanceTasks
            .Include(t => t.Property)
            .OrderBy(t => t.Title)
            .ToListAsync();

    public new async Task<MaintenanceTask?> GetByIdAsync(Guid id) =>
        await Context.MaintenanceTasks
            .Include(t => t.Property)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<List<MaintenanceTask>> GetByPropertyAsync(Guid? propertyId)
    {
        var query = Context.MaintenanceTasks
            .Include(t => t.Property)
            .AsQueryable();

        if (propertyId.HasValue)
            query = query.Where(t => t.PropertyId == propertyId.Value);

        return await query.OrderBy(t => t.Title).ToListAsync();
    }

    public async Task<List<MaintenanceTask>> GetOverdueOrUpcomingAsync(int withinDays)
    {
        var active = await Context.MaintenanceTasks
            .Include(t => t.Property)
            .Where(t => t.IsActive)
            .ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var cutoff = today.AddDays(withinDays);

        return [.. active
            .Select(t => new { Task = t, NextDue = ComputeNextDueDate(t) })
            .Where(x => x.NextDue <= cutoff)
            .OrderBy(x => x.NextDue)
            .Select(x => x.Task)];
    }

    public async Task<MaintenanceTask?> GetWithLogsAsync(Guid id) =>
        await Context.MaintenanceTasks
            .Include(t => t.Property)
            .Include(t => t.Logs.OrderByDescending(l => l.CompletedDate))
            .FirstOrDefaultAsync(t => t.Id == id);

    private static DateOnly ComputeNextDueDate(MaintenanceTask task)
    {
        var anchor = task.LastCompletedDate ?? DateOnly.FromDateTime(task.CreatedAt);
        return task.Interval switch
        {
            RecurrenceInterval.Days => anchor.AddDays(task.IntervalValue),
            RecurrenceInterval.Weeks => anchor.AddDays(task.IntervalValue * 7),
            RecurrenceInterval.Months => anchor.AddMonths(task.IntervalValue),
            RecurrenceInterval.Years => anchor.AddYears(task.IntervalValue),
            _ => anchor
        };
    }
}

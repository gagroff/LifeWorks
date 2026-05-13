using LifeWorks.Application.Repositories;
using LifeWorks.Application.Services;
using LifeWorks.Domain.Entities;
using LifeWorks.Domain.Enums;
using NSubstitute;

namespace LifeWorks.Application.Tests;

public class MaintenanceServiceTests
{
    private readonly IMaintenanceTaskRepository _taskRepo = Substitute.For<IMaintenanceTaskRepository>();
    private readonly IMaintenanceLogRepository _logRepo = Substitute.For<IMaintenanceLogRepository>();
    private readonly MaintenanceService _sut;

    public MaintenanceServiceTests() => _sut = new MaintenanceService(_taskRepo, _logRepo);

    [Fact]
    public async Task AddTask_SetsTimestamps()
    {
        var task = new MaintenanceTask
        {
            Title = "Change HVAC Filter",
            PropertyId = Guid.NewGuid(),
            Interval = RecurrenceInterval.Days,
            IntervalValue = 90
        };
        var before = DateTime.UtcNow;

        await _sut.AddTaskAsync(task);

        Assert.NotEqual(Guid.Empty, task.Id);
        Assert.True(task.CreatedAt >= before);
        Assert.True(task.UpdatedAt >= before);
        await _taskRepo.Received(1).AddAsync(task);
        await _taskRepo.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task LogCompletion_UpdatesLastCompletedDate()
    {
        var taskId = Guid.NewGuid();
        var task = new MaintenanceTask
        {
            Id = taskId,
            Title = "Inspect Roof",
            Interval = RecurrenceInterval.Years,
            IntervalValue = 1
        };
        _taskRepo.GetByIdAsync(taskId).Returns(task);
        var completed = new DateOnly(2026, 5, 1);

        await _sut.LogCompletionAsync(taskId, completed, cost: 200m, notes: "OK");

        Assert.Equal(completed, task.LastCompletedDate);
        await _taskRepo.Received(1).UpdateAsync(task);
        await _taskRepo.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task LogCompletion_CreatesLogEntry()
    {
        var taskId = Guid.NewGuid();
        var task = new MaintenanceTask { Id = taskId, Title = "Clean Gutters", Interval = RecurrenceInterval.Months, IntervalValue = 6 };
        _taskRepo.GetByIdAsync(taskId).Returns(task);
        var completed = new DateOnly(2026, 4, 15);

        await _sut.LogCompletionAsync(taskId, completed, cost: 75m, notes: "Front and back");

        await _logRepo.Received(1).AddAsync(Arg.Is<MaintenanceLog>(l =>
            l.MaintenanceTaskId == taskId &&
            l.CompletedDate == completed &&
            l.Cost == 75m &&
            l.Notes == "Front and back" &&
            l.Id != Guid.Empty));
    }

    [Fact]
    public async Task GetOverdueOrUpcoming_ReturnsTasksDueWithinWindow()
    {
        var expected = new List<MaintenanceTask>
        {
            new() { Title = "Replace Smoke Detector Batteries", Interval = RecurrenceInterval.Months, IntervalValue = 12 }
        };
        _taskRepo.GetOverdueOrUpcomingAsync(30).Returns(expected);

        var result = await _sut.GetOverdueOrUpcomingAsync(30);

        Assert.Equal(expected, result);
        await _taskRepo.Received(1).GetOverdueOrUpcomingAsync(30);
    }

    [Fact]
    public async Task DeleteTask_WhenNotFound_DoesNotThrow()
    {
        var id = Guid.NewGuid();
        _taskRepo.GetByIdAsync(id).Returns((MaintenanceTask?)null);

        await _sut.DeleteTaskAsync(id);

        await _taskRepo.DidNotReceive().DeleteAsync(Arg.Any<MaintenanceTask>());
        await _taskRepo.DidNotReceive().SaveChangesAsync();
    }
}

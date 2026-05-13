namespace LifeWorks.Domain.Entities;

public class MaintenanceLog
{
    public Guid Id { get; set; }
    public Guid MaintenanceTaskId { get; set; }
    public DateOnly CompletedDate { get; set; }
    public decimal? Cost { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    public MaintenanceTask Task { get; set; } = null!;
}

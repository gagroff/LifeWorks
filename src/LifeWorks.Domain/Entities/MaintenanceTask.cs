using System.ComponentModel.DataAnnotations;
using LifeWorks.Domain.Enums;

namespace LifeWorks.Domain.Entities;

public class MaintenanceTask
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }

    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public RecurrenceInterval Interval { get; set; }
    public int IntervalValue { get; set; }

    public DateOnly? LastCompletedDate { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Property Property { get; set; } = null!;
    public ICollection<MaintenanceLog> Logs { get; set; } = [];
}

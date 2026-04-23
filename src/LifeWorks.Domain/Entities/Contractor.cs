using System.ComponentModel.DataAnnotations;

namespace LifeWorks.Domain.Entities;

public class Contractor
{
    public Guid Id { get; set; }
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<HomeImprovement> HomeImprovements { get; set; } = [];
}

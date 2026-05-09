using System.ComponentModel.DataAnnotations;

namespace LifeWorks.Domain.Entities;

public class Asset
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(200)]
    public string? Make { get; set; }

    [MaxLength(200)]
    public string? Model { get; set; }

    [MaxLength(200)]
    public string? SerialNumber { get; set; }

    public DateOnly? PurchaseDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public decimal? EstimatedValue { get; set; }
    public DateOnly? WarrantyExpiration { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Property Property { get; set; } = null!;
}
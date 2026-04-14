namespace LifeWorks.Domain.Entities;

public class HomeImprovement
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? ContractorId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? DetailedNotes { get; set; }
    public DateOnly DateCompleted { get; set; }
    public decimal? Cost { get; set; }

    public DateOnly? WarrantyExpiration { get; set; }

    public string? ManufacturerName { get; set; }
    public string? ManufacturerModel { get; set; }
    public string? ManufacturerSerialNumber { get; set; }
    public DateOnly? ManufacturerWarrantyExpiration { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Property Property { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public Contractor? Contractor { get; set; }
}

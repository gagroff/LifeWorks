namespace LifeWorks.Domain.Entities;

#pragma warning disable CA1716 // "Property" is a valid domain term; CA1716 flags VB.NET keyword conflicts
public class Property
#pragma warning restore CA1716
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<HomeImprovement> HomeImprovements { get; set; } = [];
}

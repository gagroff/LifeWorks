namespace LifeWorks.Domain.Entities;

public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsSeeded { get; set; }

    public ICollection<HomeImprovement> HomeImprovements { get; set; } = [];
}

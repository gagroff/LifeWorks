namespace LifeWorks.Application.Models;

public class HomeImprovementFilter
{
    public Guid? PropertyId { get; set; }
    public Guid? CategoryId { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}

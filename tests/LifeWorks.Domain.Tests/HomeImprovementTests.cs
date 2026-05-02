using LifeWorks.Domain.Entities;

namespace LifeWorks.Domain.Tests;

public class HomeImprovementTests
{
    [Fact]
    public void ContractorId_IsNullByDefault()
    {
        var improvement = new HomeImprovement();

        Assert.Null(improvement.ContractorId);
    }

    [Fact]
    public void Contractor_OptionalFields_AreNullByDefault()
    {
        var improvement = new HomeImprovement();

        Assert.Null(improvement.Contractor);
        Assert.Null(improvement.DetailedNotes);
        Assert.Null(improvement.Cost);
        Assert.Null(improvement.WarrantyExpiration);
        Assert.Null(improvement.ManufacturerName);
        Assert.Null(improvement.ManufacturerModel);
        Assert.Null(improvement.ManufacturerSerialNumber);
        Assert.Null(improvement.ManufacturerWarrantyExpiration);
    }
}

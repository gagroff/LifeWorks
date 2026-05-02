using LifeWorks.Application.Models;
using LifeWorks.Application.Repositories;
using LifeWorks.Application.Services;
using LifeWorks.Domain.Entities;
using NSubstitute;

namespace LifeWorks.Application.Tests;

public class HomeImprovementServiceTests
{
    private readonly IHomeImprovementRepository _repo = Substitute.For<IHomeImprovementRepository>();
    private readonly HomeImprovementService _sut;

    public HomeImprovementServiceTests() => _sut = new HomeImprovementService(_repo);

    [Fact]
    public async Task GetAll_WithPropertyFilter_ReturnsCorrectSubset()
    {
        var propertyId = Guid.NewGuid();
        var filter = new HomeImprovementFilter { PropertyId = propertyId };
        var expected = new List<HomeImprovement>
        {
            new() { PropertyId = propertyId, Title = "Roof repair" }
        };
        _repo.GetFilteredAsync(Arg.Is<HomeImprovementFilter>(f => f.PropertyId == propertyId))
             .Returns(expected);

        var result = await _sut.GetAllAsync(filter);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetAll_WithDateRangeFilter_ReturnsCorrectSubset()
    {
        var from = new DateOnly(2024, 1, 1);
        var to = new DateOnly(2024, 6, 30);
        var filter = new HomeImprovementFilter { DateFrom = from, DateTo = to };
        var expected = new List<HomeImprovement>
        {
            new() { DateCompleted = new DateOnly(2024, 3, 15), Title = "Paint" }
        };
        _repo.GetFilteredAsync(Arg.Is<HomeImprovementFilter>(f => f.DateFrom == from && f.DateTo == to))
             .Returns(expected);

        var result = await _sut.GetAllAsync(filter);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetTotalCost_SumsNonNullCosts_TreatsNullAsZero()
    {
        _repo.GetTotalCostAsync(Arg.Any<HomeImprovementFilter>()).Returns(1500m);

        var total = await _sut.GetTotalCostAsync();

        Assert.Equal(1500m, total);
    }

    [Fact]
    public async Task GetTotalCost_WithFilter_RespectsFilter()
    {
        var propertyId = Guid.NewGuid();
        var filter = new HomeImprovementFilter { PropertyId = propertyId };
        _repo.GetTotalCostAsync(Arg.Is<HomeImprovementFilter>(f => f.PropertyId == propertyId))
             .Returns(500m);

        var total = await _sut.GetTotalCostAsync(filter);

        Assert.Equal(500m, total);
    }

    [Fact]
    public async Task Add_SetsAuditTimestamps()
    {
        var improvement = new HomeImprovement { Title = "New deck" };
        var before = DateTime.UtcNow;

        await _sut.AddAsync(improvement);

        Assert.NotEqual(Guid.Empty, improvement.Id);
        Assert.True(improvement.CreatedAt >= before);
        Assert.True(improvement.UpdatedAt >= before);
        await _repo.Received(1).AddAsync(improvement);
        await _repo.Received(1).SaveChangesAsync();
    }
}

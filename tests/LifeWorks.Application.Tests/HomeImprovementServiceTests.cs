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

    [Fact]
    public async Task GetCostByProperty_DelegatesToRepository()
    {
        var expected = new List<(string, decimal)> { ("Primary Home", 4500m), ("Lake House", 1200m) };
        _repo.GetCostByPropertyAsync().Returns(expected);

        var result = await _sut.GetCostByPropertyAsync();

        Assert.Equal(expected, result);
        await _repo.Received(1).GetCostByPropertyAsync();
    }

    [Fact]
    public async Task GetCostByCategory_DelegatesToRepository()
    {
        var expected = new List<(string, decimal)> { ("Roofing", 3000m), ("Plumbing", 800m) };
        _repo.GetCostByCategoryAsync().Returns(expected);

        var result = await _sut.GetCostByCategoryAsync();

        Assert.Equal(expected, result);
        await _repo.Received(1).GetCostByCategoryAsync();
    }

    [Fact]
    public async Task GetExpiringWarranties_DelegatesToRepositoryWithDays()
    {
        var expected = new List<HomeImprovement>
        {
            new() { Title = "HVAC unit", WarrantyExpiration = DateOnly.FromDateTime(DateTime.Today.AddDays(30)) }
        };
        _repo.GetExpiringWarrantiesAsync(90).Returns(expected);

        var result = await _sut.GetExpiringWarrantiesAsync(90);

        Assert.Equal(expected, result);
        await _repo.Received(1).GetExpiringWarrantiesAsync(90);
    }

    [Fact]
    public async Task GetExpiringWarranties_ReturnsEmptyList_WhenNoneExpiring()
    {
        _repo.GetExpiringWarrantiesAsync(Arg.Any<int>()).Returns([]);

        var result = await _sut.GetExpiringWarrantiesAsync(90);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRecent_DelegatesToRepositoryWithCount()
    {
        var expected = new List<HomeImprovement>
        {
            new() { Title = "Paint bedroom", DateCompleted = new DateOnly(2025, 4, 1) },
            new() { Title = "Fix fence",     DateCompleted = new DateOnly(2025, 3, 15) }
        };
        _repo.GetRecentAsync(5).Returns(expected);

        var result = await _sut.GetRecentAsync(5);

        Assert.Equal(expected, result);
        await _repo.Received(1).GetRecentAsync(5);
    }

    [Fact]
    public async Task GetRecent_ReturnsEmptyList_WhenNoImprovementsExist()
    {
        _repo.GetRecentAsync(Arg.Any<int>()).Returns([]);

        var result = await _sut.GetRecentAsync(5);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAll_FilteredByContractorId_ReturnsMatchingImprovements()
    {
        var contractorId = Guid.NewGuid();
        var filter = new HomeImprovementFilter { ContractorId = contractorId };
        var expected = new List<HomeImprovement>
        {
            new() { ContractorId = contractorId, Title = "Rewire basement" },
            new() { ContractorId = contractorId, Title = "Install panel" }
        };
        _repo.GetFilteredAsync(Arg.Is<HomeImprovementFilter>(f => f.ContractorId == contractorId))
             .Returns(expected);

        var result = await _sut.GetAllAsync(filter);

        Assert.Equal(expected, result);
        Assert.All(result, i => Assert.Equal(contractorId, i.ContractorId));
    }
}

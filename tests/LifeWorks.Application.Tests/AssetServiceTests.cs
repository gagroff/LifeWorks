using LifeWorks.Application.Repositories;
using LifeWorks.Application.Services;
using LifeWorks.Domain.Entities;
using NSubstitute;

namespace LifeWorks.Application.Tests;

public class AssetServiceTests
{
    private readonly IAssetRepository _repo = Substitute.For<IAssetRepository>();
    private readonly AssetService _sut;

    public AssetServiceTests() => _sut = new AssetService(_repo);

    [Fact]
    public async Task Add_SetsTimestamps()
    {
        var asset = new Asset { Name = "Water Heater", PropertyId = Guid.NewGuid() };
        var before = DateTime.UtcNow;

        await _sut.AddAsync(asset);

        Assert.NotEqual(Guid.Empty, asset.Id);
        Assert.True(asset.CreatedAt >= before);
        Assert.True(asset.UpdatedAt >= before);
        await _repo.Received(1).AddAsync(asset);
        await _repo.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task Delete_WhenNotFound_DoesNotThrow()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id).Returns((Asset?)null);

        await _sut.DeleteAsync(id);

        await _repo.DidNotReceive().DeleteAsync(Arg.Any<Asset>());
        await _repo.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task GetExpiringWarranties_ReturnsAssetsWithinWindow()
    {
        var expected = new List<Asset>
        {
            new()
            {
                Name = "Dishwasher",
                WarrantyExpiration = DateOnly.FromDateTime(DateTime.Today.AddDays(20))
            }
        };
        _repo.GetExpiringWarrantiesAsync(90).Returns(expected);

        var result = await _sut.GetExpiringWarrantiesAsync(90);

        Assert.Equal(expected, result);
        await _repo.Received(1).GetExpiringWarrantiesAsync(90);
    }

    [Fact]
    public async Task GetByProperty_ReturnsOnlyMatchingPropertyAssets()
    {
        var propertyId = Guid.NewGuid();
        var expected = new List<Asset>
        {
            new() { PropertyId = propertyId, Name = "HVAC Unit" },
            new() { PropertyId = propertyId, Name = "Garage Door Opener" }
        };
        _repo.GetByPropertyAsync(propertyId).Returns(expected);

        var result = await _sut.GetAllByPropertyAsync(propertyId);

        Assert.Equal(expected, result);
        Assert.All(result, a => Assert.Equal(propertyId, a.PropertyId));
        await _repo.Received(1).GetByPropertyAsync(propertyId);
    }
}

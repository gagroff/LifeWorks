using LifeWorks.Application.Repositories;
using LifeWorks.Application.Services;
using LifeWorks.Domain.Entities;
using NSubstitute;

namespace LifeWorks.Application.Tests;

public class ContractorServiceTests
{
    private readonly IContractorRepository _repo = Substitute.For<IContractorRepository>();
    private readonly ContractorService _sut;

    public ContractorServiceTests() => _sut = new ContractorService(_repo);

    [Fact]
    public async Task GetAll_WithSearchTerm_FiltersCorrectly()
    {
        var term = "plumber";
        var expected = new List<Contractor> { new() { Name = "Bob the Plumber" } };
        _repo.SearchAsync(term).Returns(expected);

        var result = await _sut.GetAllAsync(term);

        Assert.Equal(expected, result);
        await _repo.Received(1).SearchAsync(term);
    }

    [Fact]
    public async Task Delete_WithLinkedImprovement_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        _repo.GetLinkedImprovementCountAsync(id).Returns(2);

        var result = await _sut.DeleteAsync(id);

        Assert.False(result);
        await _repo.DidNotReceive().DeleteAsync(Arg.Any<Contractor>());
    }

    [Fact]
    public async Task Delete_Unlinked_Succeeds()
    {
        var id = Guid.NewGuid();
        var contractor = new Contractor { Id = id };
        _repo.GetLinkedImprovementCountAsync(id).Returns(0);
        _repo.GetByIdAsync(id).Returns(contractor);

        var result = await _sut.DeleteAsync(id);

        Assert.True(result);
        await _repo.Received(1).DeleteAsync(contractor);
        await _repo.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task Add_SetsAuditTimestamps()
    {
        var contractor = new Contractor { Name = "Alice" };
        var before = DateTime.UtcNow;

        await _sut.AddAsync(contractor);

        Assert.NotEqual(Guid.Empty, contractor.Id);
        Assert.True(contractor.CreatedAt >= before);
        Assert.True(contractor.UpdatedAt >= before);
        await _repo.Received(1).AddAsync(contractor);
        await _repo.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task GetFavorites_ReturnsOnlyFavoriteContractors()
    {
        var favorites = new List<Contractor>
        {
            new() { Name = "Alice", IsFavorite = true },
            new() { Name = "Bob",   IsFavorite = true }
        };
        _repo.GetFavoritesAsync().Returns(favorites);

        var result = await _sut.GetFavoritesAsync();

        Assert.Equal(favorites, result);
        Assert.All(result, c => Assert.True(c.IsFavorite));
        await _repo.Received(1).GetFavoritesAsync();
    }

    [Fact]
    public async Task GetDistinctTrades_ReturnsUniqueValues()
    {
        var trades = new List<string> { "Electrical", "HVAC", "Plumbing" };
        _repo.GetDistinctTradesAsync().Returns(trades);

        var result = await _sut.GetDistinctTradesAsync();

        Assert.Equal(trades, result);
        Assert.Equal(result.Count, result.Distinct().Count());
        await _repo.Received(1).GetDistinctTradesAsync();
    }
}

using LifeWorks.Application.Repositories;
using LifeWorks.Application.Services;
using LifeWorks.Domain.Entities;
using NSubstitute;

namespace LifeWorks.Application.Tests;

public class CategoryServiceTests
{
    private readonly ICategoryRepository _repo = Substitute.For<ICategoryRepository>();
    private readonly CategoryService _sut;

    public CategoryServiceTests() => _sut = new CategoryService(_repo);

    [Fact]
    public async Task GetAll_ReturnsOrderedBySortOrder()
    {
        var categories = new List<Category>
        {
            new() { Id = Guid.NewGuid(), Name = "B", SortOrder = 2 },
            new() { Id = Guid.NewGuid(), Name = "A", SortOrder = 1 },
        };
        _repo.GetAllOrderedAsync().Returns(categories);

        var result = await _sut.GetAllAsync();

        Assert.Equal(categories, result);
        await _repo.Received(1).GetAllOrderedAsync();
    }

    [Fact]
    public async Task Add_AssignsNewGuidAndIsSeededFalse()
    {
        var category = new Category { Name = "Test", SortOrder = 99 };

        await _sut.AddAsync(category);

        Assert.NotEqual(Guid.Empty, category.Id);
        Assert.False(category.IsSeeded);
        await _repo.Received(1).AddAsync(category);
        await _repo.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task Delete_SeededCategory_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id).Returns(new Category { Id = id, IsSeeded = true });

        var result = await _sut.DeleteAsync(id);

        Assert.False(result);
        await _repo.DidNotReceive().DeleteAsync(Arg.Any<Category>());
    }

    [Fact]
    public async Task Delete_CategoryInUse_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id).Returns(new Category { Id = id, IsSeeded = false });
        _repo.HasLinkedImprovementsAsync(id).Returns(true);

        var result = await _sut.DeleteAsync(id);

        Assert.False(result);
        await _repo.DidNotReceive().DeleteAsync(Arg.Any<Category>());
    }

    [Fact]
    public async Task Delete_CustomUnusedCategory_Succeeds()
    {
        var id = Guid.NewGuid();
        var category = new Category { Id = id, IsSeeded = false };
        _repo.GetByIdAsync(id).Returns(category);
        _repo.HasLinkedImprovementsAsync(id).Returns(false);

        var result = await _sut.DeleteAsync(id);

        Assert.True(result);
        await _repo.Received(1).DeleteAsync(category);
        await _repo.Received(1).SaveChangesAsync();
    }
}

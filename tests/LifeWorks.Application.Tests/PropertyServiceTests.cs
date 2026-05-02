using LifeWorks.Application.Repositories;
using LifeWorks.Application.Services;
using LifeWorks.Domain.Entities;
using NSubstitute;

namespace LifeWorks.Application.Tests;

public class PropertyServiceTests
{
    private readonly IPropertyRepository _repo = Substitute.For<IPropertyRepository>();
    private readonly PropertyService _sut;

    public PropertyServiceTests() => _sut = new PropertyService(_repo);

    [Fact]
    public async Task Add_SetsNewGuidAndAuditTimestamps()
    {
        var entity = new Property { Name = "Beach House", Address = "123 Ocean Dr" };
        var before = DateTime.UtcNow;

        await _sut.AddAsync(entity);

        Assert.NotEqual(Guid.Empty, entity.Id);
        Assert.True(entity.CreatedAt >= before);
        Assert.True(entity.UpdatedAt >= before);
        await _repo.Received(1).AddAsync(entity);
        await _repo.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task Update_SetsUpdatedAt_AndSaves()
    {
        var entity = new Property { Id = Guid.NewGuid(), Name = "Lake House", CreatedAt = DateTime.UtcNow.AddDays(-10) };
        var before = DateTime.UtcNow;

        await _sut.UpdateAsync(entity);

        Assert.True(entity.UpdatedAt >= before);
        await _repo.Received(1).UpdateAsync(entity);
        await _repo.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task Delete_WhenPropertyHasImprovements_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        _repo.HasImprovementsAsync(id).Returns(true);

        var result = await _sut.DeleteAsync(id);

        Assert.False(result);
        await _repo.DidNotReceive().DeleteAsync(Arg.Any<Property>());
        await _repo.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task Delete_WhenPropertyNotFound_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        _repo.HasImprovementsAsync(id).Returns(false);
        _repo.GetByIdAsync(id).Returns((Property?)null);

        var result = await _sut.DeleteAsync(id);

        Assert.False(result);
        await _repo.DidNotReceive().DeleteAsync(Arg.Any<Property>());
    }

    [Fact]
    public async Task Delete_WhenPropertyExistsWithNoImprovements_ReturnsTrueAndDeletes()
    {
        var id = Guid.NewGuid();
        var entity = new Property { Id = id, Name = "Primary Home" };
        _repo.HasImprovementsAsync(id).Returns(false);
        _repo.GetByIdAsync(id).Returns(entity);

        var result = await _sut.DeleteAsync(id);

        Assert.True(result);
        await _repo.Received(1).DeleteAsync(entity);
        await _repo.Received(1).SaveChangesAsync();
    }
}

using LifeWorks.Application.Repositories;
using LifeWorks.Application.Services;
using LifeWorks.Application.Storage;
using LifeWorks.Domain.Entities;
using NSubstitute;

namespace LifeWorks.Application.Tests;

public class AttachmentServiceTests
{
    private readonly IAttachmentRepository _repo = Substitute.For<IAttachmentRepository>();
    private readonly IFileStorage _fileStorage = Substitute.For<IFileStorage>();
    private readonly AttachmentService _sut;

    public AttachmentServiceTests()
    {
        _sut = new AttachmentService(_repo, _fileStorage);
    }

    [Fact]
    public async Task SaveAsync_CreatesAttachmentRecordWithCorrectMetadata()
    {
        var improvementId = Guid.NewGuid();
        var content = "fake pdf content"u8.ToArray();
        using var stream = new MemoryStream(content);

        var result = await _sut.SaveAsync(improvementId, "receipt.pdf", "application/pdf", stream);

        Assert.Equal(improvementId, result.HomeImprovementId);
        Assert.Equal("receipt.pdf", result.FileName);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.EndsWith(".pdf", result.StoredFileName, StringComparison.OrdinalIgnoreCase);
        Assert.True(result.FileSize > 0);
    }

    [Fact]
    public async Task SaveAsync_DelegatesToFileStorage()
    {
        var content = "hello world"u8.ToArray();
        using var stream = new MemoryStream(content);

        var result = await _sut.SaveAsync(Guid.NewGuid(), "note.txt", "text/plain", stream);

        await _fileStorage.Received(1).SaveAsync(
            result.StoredFileName,
            Arg.Any<Stream>(),
            "text/plain",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_PersistsRecordViaRepository()
    {
        using var stream = new MemoryStream("data"u8.ToArray());

        await _sut.SaveAsync(Guid.NewGuid(), "file.jpg", "image/jpeg", stream);

        await _repo.Received(1).AddAsync(Arg.Any<Attachment>());
        await _repo.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task DeleteAsync_DelegatesToFileStorage()
    {
        var attachment = new Attachment { Id = Guid.NewGuid(), StoredFileName = "stored.pdf" };
        _repo.GetByIdAsync(attachment.Id).Returns(attachment);

        await _sut.DeleteAsync(attachment.Id);

        await _fileStorage.Received(1).DeleteAsync("stored.pdf", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_DeletesRecordViaRepository()
    {
        var attachment = new Attachment { Id = Guid.NewGuid(), StoredFileName = "stored.pdf" };
        _repo.GetByIdAsync(attachment.Id).Returns(attachment);

        await _sut.DeleteAsync(attachment.Id);

        await _repo.Received(1).DeleteAsync(attachment);
        await _repo.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task DeleteAsync_WhenAttachmentNotFound_DoesNothing()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>()).Returns((Attachment?)null);

        await _sut.DeleteAsync(Guid.NewGuid());

        await _repo.DidNotReceive().DeleteAsync(Arg.Any<Attachment>());
        await _fileStorage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByImprovementIdAsync_DelegatesToRepository()
    {
        var improvementId = Guid.NewGuid();
        var expected = new List<Attachment> { new() { Id = Guid.NewGuid(), HomeImprovementId = improvementId } };
        _repo.GetByImprovementIdAsync(improvementId).Returns(expected);

        var result = await _sut.GetByImprovementIdAsync(improvementId);

        Assert.Equal(expected, result);
    }
}

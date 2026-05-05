using LifeWorks.Application.Repositories;
using LifeWorks.Application.Services;
using LifeWorks.Domain.Entities;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace LifeWorks.Application.Tests;

public class AttachmentServiceTests : IDisposable
{
    private readonly IAttachmentRepository _repo = Substitute.For<IAttachmentRepository>();
    private readonly string _uploadPath;
    private readonly AttachmentService _sut;

    public AttachmentServiceTests()
    {
        _uploadPath = Path.Combine(Path.GetTempPath(), "lifeworks-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_uploadPath);

        var config = Substitute.For<IConfiguration>();
        config["Attachments:UploadPath"].Returns(_uploadPath);

        _sut = new AttachmentService(_repo, config);
    }

    public void Dispose()
    {
        if (Directory.Exists(_uploadPath))
            Directory.Delete(_uploadPath, recursive: true);
        GC.SuppressFinalize(this);
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
    public async Task SaveAsync_WritesFileToDisk()
    {
        var content = "hello world"u8.ToArray();
        using var stream = new MemoryStream(content);

        var result = await _sut.SaveAsync(Guid.NewGuid(), "note.txt", "text/plain", stream);

        var fullPath = Path.Combine(_uploadPath, result.StoredFileName);
        Assert.True(File.Exists(fullPath));
        Assert.Equal(content, await File.ReadAllBytesAsync(fullPath));
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
    public async Task DeleteAsync_RemovesFileFromDisk()
    {
        var fileName = Guid.NewGuid() + ".pdf";
        var fullPath = Path.Combine(_uploadPath, fileName);
        await File.WriteAllTextAsync(fullPath, "content");

        var attachment = new Attachment { Id = Guid.NewGuid(), StoredFileName = fileName };
        _repo.GetByIdAsync(attachment.Id).Returns(attachment);

        await _sut.DeleteAsync(attachment.Id);

        Assert.False(File.Exists(fullPath));
    }

    [Fact]
    public async Task DeleteAsync_DeletesRecordViaRepository()
    {
        var fileName = Guid.NewGuid() + ".pdf";
        var fullPath = Path.Combine(_uploadPath, fileName);
        await File.WriteAllTextAsync(fullPath, "content");

        var attachment = new Attachment { Id = Guid.NewGuid(), StoredFileName = fileName };
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

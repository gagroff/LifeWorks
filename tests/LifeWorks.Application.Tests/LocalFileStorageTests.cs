using LifeWorks.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;

namespace LifeWorks.Application.Tests;

public sealed class LocalFileStorageTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly LocalFileStorage _storage;

    public LocalFileStorageTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Attachments:UploadPath"] = _tempDir })
            .Build();
        _storage = new LocalFileStorage(config);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task Save_ThenOpenRead_ReturnsSameBytes()
    {
        var data = "hello world"u8.ToArray();
        await _storage.SaveAsync("test.txt", new MemoryStream(data), "text/plain");
        await using var stream = await _storage.OpenReadAsync("test.txt");
        var result = new MemoryStream();
        await stream.CopyToAsync(result);
        Assert.Equal(data, result.ToArray());
    }

    [Fact]
    public async Task Delete_RemovesFile()
    {
        var data = "data"u8.ToArray();
        await _storage.SaveAsync("file.txt", new MemoryStream(data), "text/plain");
        await _storage.DeleteAsync("file.txt");
        Assert.False(File.Exists(Path.Combine(_tempDir, "file.txt")));
    }

    [Fact]
    public async Task Delete_OnMissingFile_DoesNotThrow()
    {
        await _storage.DeleteAsync("nonexistent.txt"); // should not throw
    }

    [Fact]
    public async Task OpenRead_OnMissingFile_ThrowsFileNotFoundException()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(() => _storage.OpenReadAsync("ghost.txt"));
    }
}

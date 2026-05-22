using LifeWorks.Application.Storage;
using Microsoft.Extensions.Configuration;

namespace LifeWorks.Infrastructure.Storage;

public sealed class LocalFileStorage(IConfiguration configuration) : IFileStorage
{
    private string Root => configuration["Attachments:UploadPath"] ?? Path.Combine("wwwroot", "uploads");

    public async Task SaveAsync(string storedName, Stream content, string contentType, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Root);
        var fullPath = Path.Combine(Root, storedName);
        await using var dest = File.Create(fullPath);
        await content.CopyToAsync(dest, ct);
    }

    public Task<Stream> OpenReadAsync(string storedName, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(Root, storedName);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {storedName}", storedName);
        return Task.FromResult<Stream>(File.OpenRead(fullPath));
    }

    public Task DeleteAsync(string storedName, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(Root, storedName);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }
}

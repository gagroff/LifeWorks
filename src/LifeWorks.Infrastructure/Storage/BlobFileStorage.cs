using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LifeWorks.Application.Storage;
using Microsoft.Extensions.Configuration;

namespace LifeWorks.Infrastructure.Storage;

public sealed class BlobFileStorage : IFileStorage
{
    private readonly BlobContainerClient _container;
    private bool _containerEnsured;

    public BlobFileStorage(BlobServiceClient blobServiceClient, IConfiguration configuration)
    {
        var containerName = configuration["Storage:AttachmentsContainer"] ?? "attachments";
        _container = blobServiceClient.GetBlobContainerClient(containerName);
    }

    private async Task EnsureContainerAsync(CancellationToken ct)
    {
        if (_containerEnsured) return;
        await _container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
        _containerEnsured = true;
    }

    public async Task SaveAsync(string storedName, Stream content, string contentType, CancellationToken ct = default)
    {
        await EnsureContainerAsync(ct);
        var blob = _container.GetBlobClient(storedName);
        await blob.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);
    }

    public async Task<Stream> OpenReadAsync(string storedName, CancellationToken ct = default)
    {
        await EnsureContainerAsync(ct);
        var blob = _container.GetBlobClient(storedName);
        try
        {
            return await blob.OpenReadAsync(cancellationToken: ct);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            throw new FileNotFoundException($"Blob not found: {storedName}", storedName);
        }
    }

    public async Task DeleteAsync(string storedName, CancellationToken ct = default)
    {
        await EnsureContainerAsync(ct);
        var blob = _container.GetBlobClient(storedName);
        await blob.DeleteIfExistsAsync(cancellationToken: ct);
    }
}

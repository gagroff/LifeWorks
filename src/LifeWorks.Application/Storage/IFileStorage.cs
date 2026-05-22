namespace LifeWorks.Application.Storage;

public interface IFileStorage
{
    Task SaveAsync(string storedName, Stream content, string contentType, CancellationToken ct = default);
    Task<Stream> OpenReadAsync(string storedName, CancellationToken ct = default);
    Task DeleteAsync(string storedName, CancellationToken ct = default);
}

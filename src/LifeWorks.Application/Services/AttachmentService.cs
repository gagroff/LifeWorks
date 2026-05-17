using LifeWorks.Application.Repositories;
using LifeWorks.Application.Storage;
using LifeWorks.Domain.Entities;

namespace LifeWorks.Application.Services;

public class AttachmentService(IAttachmentRepository repository, IFileStorage fileStorage) : IAttachmentService
{
    public Task<List<Attachment>> GetByImprovementIdAsync(Guid improvementId) =>
        repository.GetByImprovementIdAsync(improvementId);

    public async Task<Attachment> SaveAsync(Guid improvementId, string fileName, string contentType, Stream fileStream)
    {
        var storedFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";

        using var buffer = new MemoryStream();
        await fileStream.CopyToAsync(buffer);
        var fileSize = buffer.Length;
        buffer.Position = 0;

        await fileStorage.SaveAsync(storedFileName, buffer, contentType);

        var attachment = new Attachment
        {
            Id = Guid.NewGuid(),
            HomeImprovementId = improvementId,
            FileName = fileName,
            StoredFileName = storedFileName,
            FileSize = fileSize,
            ContentType = contentType,
            UploadedAt = DateTime.UtcNow
        };

        await repository.AddAsync(attachment);
        await repository.SaveChangesAsync();
        return attachment;
    }

    public async Task<(Stream FileStream, Attachment Metadata)> GetFileAsync(Guid attachmentId)
    {
        var attachment = await repository.GetByIdAsync(attachmentId)
            ?? throw new FileNotFoundException($"Attachment {attachmentId} not found.");

        var stream = await fileStorage.OpenReadAsync(attachment.StoredFileName);
        return (stream, attachment);
    }

    public async Task DeleteAsync(Guid attachmentId)
    {
        var attachment = await repository.GetByIdAsync(attachmentId);
        if (attachment is null)
            return;

        await fileStorage.DeleteAsync(attachment.StoredFileName);

        await repository.DeleteAsync(attachment);
        await repository.SaveChangesAsync();
    }
}

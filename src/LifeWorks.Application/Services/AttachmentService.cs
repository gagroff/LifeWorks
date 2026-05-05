using LifeWorks.Application.Repositories;
using LifeWorks.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace LifeWorks.Application.Services;

public class AttachmentService(IAttachmentRepository repository, IConfiguration configuration) : IAttachmentService
{
    private string UploadPath => configuration["Attachments:UploadPath"] ?? Path.Combine("wwwroot", "uploads");

    public Task<List<Attachment>> GetByImprovementIdAsync(Guid improvementId) =>
        repository.GetByImprovementIdAsync(improvementId);

    public async Task<Attachment> SaveAsync(Guid improvementId, string fileName, string contentType, Stream fileStream)
    {
        Directory.CreateDirectory(UploadPath);

        var storedFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
        var fullPath = Path.Combine(UploadPath, storedFileName);

        await using (var dest = File.Create(fullPath))
            await fileStream.CopyToAsync(dest);

        var attachment = new Attachment
        {
            Id = Guid.NewGuid(),
            HomeImprovementId = improvementId,
            FileName = fileName,
            StoredFileName = storedFileName,
            FileSize = new FileInfo(fullPath).Length,
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

        var fullPath = Path.Combine(UploadPath, attachment.StoredFileName);
        return (File.OpenRead(fullPath), attachment);
    }

    public async Task DeleteAsync(Guid attachmentId)
    {
        var attachment = await repository.GetByIdAsync(attachmentId);
        if (attachment is null)
            return;

        var fullPath = Path.Combine(UploadPath, attachment.StoredFileName);
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        await repository.DeleteAsync(attachment);
        await repository.SaveChangesAsync();
    }
}

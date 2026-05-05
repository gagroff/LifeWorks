using LifeWorks.Domain.Entities;

namespace LifeWorks.Application.Services;

public interface IAttachmentService
{
    Task<List<Attachment>> GetByImprovementIdAsync(Guid improvementId);
    Task<Attachment> SaveAsync(Guid improvementId, string fileName, string contentType, Stream fileStream);
    Task<(Stream FileStream, Attachment Metadata)> GetFileAsync(Guid attachmentId);
    Task DeleteAsync(Guid attachmentId);
}

using LifeWorks.Application.Repositories;
using LifeWorks.Domain.Entities;
using LifeWorks.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LifeWorks.Infrastructure.Repositories;

public class AttachmentRepository(AppDbContext context) : RepositoryBase<Attachment>(context), IAttachmentRepository
{
    public Task<List<Attachment>> GetByImprovementIdAsync(Guid improvementId) =>
        Context.Attachments
            .Where(a => a.HomeImprovementId == improvementId)
            .OrderByDescending(a => a.UploadedAt)
            .ToListAsync();
}

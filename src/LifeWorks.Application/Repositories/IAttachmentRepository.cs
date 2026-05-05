using LifeWorks.Domain.Entities;

namespace LifeWorks.Application.Repositories;

public interface IAttachmentRepository : IRepository<Attachment>
{
    Task<List<Attachment>> GetByImprovementIdAsync(Guid improvementId);
}

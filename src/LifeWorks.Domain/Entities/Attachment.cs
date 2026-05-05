namespace LifeWorks.Domain.Entities;

public class Attachment
{
    public Guid Id { get; set; }
    public Guid HomeImprovementId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }

    public HomeImprovement HomeImprovement { get; set; } = null!;
}

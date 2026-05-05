# Plan C: Document/Photo Attachments per Improvement

## Context
Home improvement records are only as useful as the proof behind them — receipts, photos, permits, warranty cards. This was explicitly planned for Phase 2 in the PRD. This plan adds an `Attachment` entity, file storage to a local folder, and an upload/download UI on the improvement detail page.

---

## Phase 1 — Domain & Infrastructure: New Entity + Migration

- [x] **Task C1.1** — Create `Attachment` domain entity
  - **File:** `src/LifeWorks.Domain/Entities/Attachment.cs`
  - ```csharp
    public class Attachment
    {
        public Guid Id { get; set; }
        public Guid HomeImprovementId { get; set; }
        public string FileName { get; set; } = string.Empty;        // original file name shown to user
        public string StoredFileName { get; set; } = string.Empty;  // GUID-based name on disk
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }

        public HomeImprovement HomeImprovement { get; set; } = null!;
    }
    ```

- [x] **Task C1.2** — Add `Attachments` navigation property to `HomeImprovement`
  - **File:** `src/LifeWorks.Domain/Entities/HomeImprovement.cs`
  - Add:
    ```csharp
    public ICollection<Attachment> Attachments { get; set; } = [];
    ```

- [x] **Task C1.3** — Configure `Attachment` in `AppDbContext`
  - **File:** `src/LifeWorks.Infrastructure/Data/AppDbContext.cs`
  - Add `DbSet<Attachment> Attachments`
  - Configure relationship: cascade delete (deleting an improvement deletes its attachments)

- [x] **Task C1.4** — Add EF Core migration and update database
  - ```bash
    dotnet ef migrations add AddAttachments --project src/LifeWorks.Infrastructure --startup-project src/LifeWorks.Web
    dotnet ef database update --project src/LifeWorks.Infrastructure --startup-project src/LifeWorks.Web
    ```

---

## Phase 2 — Application Layer: Attachment Service

- [x] **Task C2.1** — Create `IAttachmentRepository`
  - **File:** `src/LifeWorks.Application/Repositories/IAttachmentRepository.cs`
  - ```csharp
    public interface IAttachmentRepository : IRepository<Attachment>
    {
        Task<List<Attachment>> GetByImprovementIdAsync(Guid improvementId);
    }
    ```

- [x] **Task C2.2** — Implement `AttachmentRepository`
  - **File:** `src/LifeWorks.Infrastructure/Repositories/AttachmentRepository.cs`
  - Extend `RepositoryBase<Attachment>`; implement `GetByImprovementIdAsync` filtering by `HomeImprovementId`

- [x] **Task C2.3** — Create `IAttachmentService`
  - **File:** `src/LifeWorks.Application/Services/IAttachmentService.cs`
  - ```csharp
    public interface IAttachmentService
    {
        Task<List<Attachment>> GetByImprovementIdAsync(Guid improvementId);
        Task<Attachment> SaveAsync(Guid improvementId, string fileName, string contentType, Stream fileStream);
        Task<(Stream FileStream, Attachment Metadata)> GetFileAsync(Guid attachmentId);
        Task DeleteAsync(Guid attachmentId);
    }
    ```

- [x] **Task C2.4** — Implement `AttachmentService`
  - **File:** `src/LifeWorks.Application/Services/AttachmentService.cs`
  - `SaveAsync`: generate GUID-based `StoredFileName`, write stream to upload folder, persist `Attachment` record
  - `GetFileAsync`: look up record, open `FileStream` from disk, return stream + metadata
  - `DeleteAsync`: delete DB record, delete file from disk
  - Upload folder read from `IConfiguration["Attachments:UploadPath"]` (default: `wwwroot/uploads`)

- [x] **Task C2.5** — Register services in DI
  - **File:** `src/LifeWorks.Infrastructure/DependencyInjection.cs`
  - Register `IAttachmentRepository` → `AttachmentRepository`
  - Register `IAttachmentService` → `AttachmentService`

---

## Phase 3 — Web Layer: Upload/Download UI

- [ ] **Task C3.1** — Include Attachments in `GetWithDetailsAsync`
  - **File:** `src/LifeWorks.Infrastructure/Repositories/HomeImprovementRepository.cs`
  - Add `.Include(h => h.Attachments)` to the `GetWithDetailsAsync` query

- [ ] **Task C3.2** — Add Attachments panel to Improvement Detail page
  - **File:** `src/LifeWorks.Web/Components/Pages/Improvements/` (detail page)
  - Add a `MudCard` or `MudExpansionPanel` labeled "Attachments"
  - List existing attachments: file name, size, upload date, download button, delete button
  - `MudFileUpload` for new uploads (accept images + PDFs)
  - On upload: call `IAttachmentService.SaveAsync`, refresh list
  - On delete: show confirmation dialog → call `IAttachmentService.DeleteAsync`, refresh list

- [ ] **Task C3.3** — Add attachment download endpoint
  - **File:** `src/LifeWorks.Web/` — minimal API endpoint at `/attachments/{id}/download`
  - Call `IAttachmentService.GetFileAsync`, return `FileStreamResult` with correct `ContentType` and `Content-Disposition: attachment`

- [ ] **Task C3.4** — Show attachment count badge on Improvements list
  - **File:** `src/LifeWorks.Web/Components/Pages/Improvements/` (index/list page)
  - Add a small chip/badge showing attachment count per row
  - Update `GetFilteredAsync` or use a count projection in the repository to avoid N+1

---

## Phase 4 — Tests

- [ ] **Task C4.1** — Unit tests for `AttachmentService`
  - **File:** `tests/LifeWorks.Application.Tests/Services/AttachmentServiceTests.cs`
  - `SaveAsync` creates correct `Attachment` record and writes to expected path
  - `DeleteAsync` removes both DB record and file from disk
  - `GetByImprovementIdAsync` returns correct records for the given improvement

---

## Verification

- [ ] `dotnet build` — no warnings
- [ ] Migration applies cleanly: `dotnet ef database update ...`
- [ ] `dotnet test` — all tests pass
- [ ] `dotnet run --project src/LifeWorks.Web`:
  - [ ] Open an improvement detail page — Attachments panel renders
  - [ ] Upload a PDF receipt — confirm it appears with correct name and file size
  - [ ] Click download — file downloads with correct content type
  - [ ] Delete the attachment — disappears from list, file removed from disk
  - [ ] Confirm improvement list shows attachment count badge per row

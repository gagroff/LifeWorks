# Plans D, E, F — LifeWorks Feature Expansion

---

# Plan D: Contractor Directory & Job History

## Context
Contractors already exist as a domain entity and are linked to HomeImprovements, but the current UI is a flat list with no specialization. This plan enriches the Contractor entity with trade/specialty tracking and a rating system, and adds a per-contractor job history view that surfaces every improvement they've worked on across all properties.

---

## Phase D1 — Domain Layer

- [x] **Task D1.1** — Add `Trade` and `Rating` fields to `Contractor`
  - **File:** `src/LifeWorks.Domain/Entities/Contractor.cs`
  - Add: `Trade` (string?, max 100) — e.g., "Plumbing", "HVAC", "General"
  - Add: `Rating` (int?) — 1–5 star scale
  - Add: `IsFavorite` (bool, default false) — quick "trusted vendor" flag

---

## Phase D2 — Infrastructure Layer

- [x] **Task D2.1** — Add EF Core migration for new Contractor columns
  - Run: `dotnet ef migrations add AddContractorTradeRating --project src/LifeWorks.Infrastructure --startup-project src/LifeWorks.Web`
  - **File:** `src/LifeWorks.Infrastructure/Migrations/` (auto-generated)

---

## Phase D3 — Application Layer

- [x] **Task D3.1** — Extend `IContractorRepository` with specialty/filter queries
  - **File:** `src/LifeWorks.Application/Repositories/IContractorRepository.cs`
  - Add: `Task<List<Contractor>> GetFavoritesAsync()`
  - Add: `Task<List<string>> GetDistinctTradesAsync()`

- [x] **Task D3.2** — Implement new repository methods
  - **File:** `src/LifeWorks.Infrastructure/Repositories/ContractorRepository.cs`
  - `GetFavoritesAsync`: filter `IsFavorite == true`, order by Name
  - `GetDistinctTradesAsync`: select distinct non-null Trade values, ordered alphabetically

- [x] **Task D3.3** — Extend `IContractorService` and `ContractorService`
  - **File:** `src/LifeWorks.Application/Services/IContractorService.cs`
  - Add: `Task<List<Contractor>> GetFavoritesAsync()`
  - Add: `Task<List<string>> GetDistinctTradesAsync()`
  - **File:** `src/LifeWorks.Application/Services/ContractorService.cs`
  - Implement both — delegate to repository

---

## Phase D4 — Web Layer

- [x] **Task D4.1** — Update `ContractorForm.razor` with new fields
  - **File:** `src/LifeWorks.Web/Components/Pages/Contractors/ContractorForm.razor`
  - Add `MudSelect` for Trade (populated from `GetDistinctTradesAsync()` + free-text option)
  - Add `MudRating` (MudBlazor component) for Rating (1–5 stars, nullable)
  - Add `MudSwitch` for IsFavorite ("Mark as Trusted Vendor")

- [x] **Task D4.2** — Update contractor list/index to show new fields
  - **File:** `src/LifeWorks.Web/Components/Pages/Contractors/Index.razor`
  - Add Trade column and star rating display to `MudDataGrid`
  - Add favorite/star icon badge on contractor row
  - Add "Favorites Only" filter toggle above the grid

- [x] **Task D4.3** — Add Contractor Detail job history view
  - **File:** `src/LifeWorks.Web/Components/Pages/Contractors/ContractorDetail.razor` (new page)
  - Route: `/contractors/{Id:guid}`
  - Sections: contractor info card (all fields + rating), then a full list of HomeImprovements linked to this contractor (title, property, category, date, cost)
  - Reuse `IHomeImprovementService.GetAllAsync()` filtered by ContractorId (extend filter)

- [x] **Task D4.4** — Extend `HomeImprovementFilter` with ContractorId
  - **File:** `src/LifeWorks.Application/Models/HomeImprovementFilter.cs`
  - Add: `Guid? ContractorId { get; set; }`
  - **File:** `src/LifeWorks.Infrastructure/Repositories/HomeImprovementRepository.cs`
  - Update `ApplyFilter` to honor ContractorId

- [x] **Task D4.5** — Update NavMenu to link contractor names to detail page
  - **File:** `src/LifeWorks.Web/Components/Pages/Contractors/Index.razor`
  - Make contractor name in grid a clickable link to `/contractors/{id}`

---

## Phase D5 — Tests

- [x] **Task D5.1** — Unit tests for new Contractor service methods
  - **File:** `tests/LifeWorks.Application.Tests/ContractorServiceTests.cs`
  - Test: `GetFavorites_ReturnsOnlyFavoriteContractors`
  - Test: `GetDistinctTrades_ReturnsUniqueValues`

- [x] **Task D5.2** — Unit tests for HomeImprovement filter with ContractorId
  - **File:** `tests/LifeWorks.Application.Tests/HomeImprovementServiceTests.cs`
  - Test: `GetAll_FilteredByContractorId_ReturnsMatchingImprovements`

---

## Verification

- [ ] `dotnet build` — zero errors/warnings
- [ ] `dotnet ef database update` — migration applies cleanly
- [ ] `dotnet test` — all tests pass
- [ ] Navigate to `/contractors` — Trade column and favorite badge visible
- [ ] Create/edit a contractor — Trade, Rating, IsFavorite fields save and reload correctly
- [ ] Click a contractor name → job history page shows all linked improvements
- [ ] Toggle "Favorites Only" — filters grid correctly

---
---

# Plan E: Home Inventory & Asset Tracking

## Context
HomeImprovement already captures appliance/manufacturer data (ManufacturerName, ManufacturerModel, ManufacturerSerialNumber, ManufacturerWarrantyExpiration) as free-form fields on improvement records. This plan introduces a dedicated `Asset` entity for cataloging durable items in each property — appliances, systems, valuables — with purchase info, estimated value, and warranty tracking independent of any improvement job. The dashboard gains an "Assets Expiring Warranty" widget.

---

## Phase E1 — Domain Layer

- [ ] **Task E1.1** — Create `Asset` entity
  - **File:** `src/LifeWorks.Domain/Entities/Asset.cs` (new)
  ```csharp
  public class Asset
  {
      public Guid Id { get; set; }
      public Guid PropertyId { get; set; }
      public string Name { get; set; } = string.Empty;       // max 200, required
      public string? Category { get; set; }                  // max 100, e.g. "Appliance", "HVAC"
      public string? Make { get; set; }                      // max 200
      public string? Model { get; set; }                     // max 200
      public string? SerialNumber { get; set; }              // max 200
      public DateOnly? PurchaseDate { get; set; }
      public decimal? PurchasePrice { get; set; }
      public decimal? EstimatedValue { get; set; }
      public DateOnly? WarrantyExpiration { get; set; }
      public string? Notes { get; set; }
      public DateTime CreatedAt { get; set; }
      public DateTime UpdatedAt { get; set; }
      public Property Property { get; set; } = null!;
  }
  ```

---

## Phase E2 — Infrastructure Layer

- [ ] **Task E2.1** — Register `Asset` in `AppDbContext`
  - **File:** `src/LifeWorks.Infrastructure/Data/AppDbContext.cs`
  - Add: `public DbSet<Asset> Assets => Set<Asset>();`
  - Configure FK: `Asset → Property` (OnDelete: Restrict)
  - Configure string max lengths and decimal precision (18, 2)

- [ ] **Task E2.2** — Add EF Core migration
  - Run: `dotnet ef migrations add AddAssets --project src/LifeWorks.Infrastructure --startup-project src/LifeWorks.Web`

---

## Phase E3 — Application Layer

- [ ] **Task E3.1** — Create `IAssetRepository`
  - **File:** `src/LifeWorks.Application/Repositories/IAssetRepository.cs` (new)
  ```csharp
  public interface IAssetRepository : IRepository<Asset>
  {
      Task<List<Asset>> GetByPropertyAsync(Guid propertyId);
      Task<List<Asset>> GetExpiringWarrantiesAsync(int withinDays);
      Task<List<string>> GetDistinctCategoriesAsync();
  }
  ```

- [ ] **Task E3.2** — Implement `AssetRepository`
  - **File:** `src/LifeWorks.Infrastructure/Repositories/AssetRepository.cs` (new)
  - Extend `RepositoryBase<Asset>`, implement all interface methods

- [ ] **Task E3.3** — Create `IAssetService` and `AssetService`
  - **File:** `src/LifeWorks.Application/Services/IAssetService.cs` (new)
  - Methods: `GetAllByPropertyAsync`, `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`, `GetExpiringWarrantiesAsync`, `GetDistinctCategoriesAsync`
  - **File:** `src/LifeWorks.Application/Services/AssetService.cs` (new)
  - Set timestamps in `AddAsync`/`UpdateAsync`; delegate all queries to repository

- [ ] **Task E3.4** — Register in DI
  - **File:** `src/LifeWorks.Infrastructure/DependencyInjection.cs`
  - Add: `services.AddScoped<IAssetRepository, AssetRepository>();`
  - **File:** `src/LifeWorks.Application/DependencyInjection.cs`
  - Add: `services.AddScoped<IAssetService, AssetService>();`

---

## Phase E4 — Web Layer

- [ ] **Task E4.1** — Asset list page per property
  - **File:** `src/LifeWorks.Web/Components/Pages/Assets/Index.razor` (new)
  - Route: `/assets`
  - Property selector (MudSelect) to filter by property
  - MudDataGrid: Name, Category, Make/Model, Purchase Date, Warranty Expiration, Estimated Value
  - Color-code warranty expiration (red < 30 days, yellow < 90 days)
  - Add/Edit/Delete actions

- [ ] **Task E4.2** — Asset form page (add/edit)
  - **File:** `src/LifeWorks.Web/Components/Pages/Assets/AssetForm.razor` (new)
  - Routes: `/assets/new`, `/assets/{Id:guid}/edit`
  - Fields: Name (required), Property (required MudSelect), Category (MudSelect from distinct categories), Make, Model, SerialNumber, PurchaseDate (MudDatePicker), PurchasePrice, EstimatedValue, WarrantyExpiration, Notes
  - 2-column responsive grid layout matching existing form pages

- [ ] **Task E4.3** — Add Assets to dashboard warranty widget
  - **File:** `src/LifeWorks.Web/Components/Pages/Home.razor`
  - Extend "Expiring Warranties" section to include assets (alongside HomeImprovements)
  - Label each row as "Asset" or "Improvement" so user can distinguish source

- [ ] **Task E4.4** — Add Assets to NavMenu
  - **File:** `src/LifeWorks.Web/Components/Layout/NavMenu.razor`
  - Add "Assets" link under the "Property Management" group (alongside Home Improvements, Contractors)

---

## Phase E5 — Tests

- [ ] **Task E5.1** — Unit tests for `AssetService`
  - **File:** `tests/LifeWorks.Application.Tests/AssetServiceTests.cs` (new)
  - Test: `Add_SetsTimestamps`
  - Test: `Delete_WhenNotFound_DoesNotThrow`
  - Test: `GetExpiringWarranties_ReturnsAssetsWithinWindow`
  - Test: `GetByProperty_ReturnsOnlyMatchingPropertyAssets`

---

## Verification

- [ ] `dotnet build` — zero errors/warnings
- [ ] `dotnet ef database update` — migration applies cleanly
- [ ] `dotnet test` — all tests pass
- [ ] Navigate to `/assets` — page loads, property filter works
- [ ] Create an asset with all fields — saves and reloads correctly
- [ ] Edit and delete an asset — both paths work
- [ ] Add an asset with warranty expiring within 30 days — appears on dashboard warranty widget
- [ ] Dashboard shows both asset and improvement warranty items labeled correctly

---
---

# Plan F: Recurring Maintenance Scheduler

## Context
LifeWorks currently tracks one-time home improvement projects but has no concept of recurring upkeep (filter changes, gutter cleaning, annual inspections). This plan adds a `MaintenanceTask` entity representing a recurring chore with a schedule, and a `MaintenanceLog` for recording each completion. The dashboard gains an "Upcoming & Overdue Maintenance" widget.

---

## Phase F1 — Domain Layer

- [ ] **Task F1.1** — Create `MaintenanceTask` entity
  - **File:** `src/LifeWorks.Domain/Entities/MaintenanceTask.cs` (new)
  ```csharp
  public class MaintenanceTask
  {
      public Guid Id { get; set; }
      public Guid PropertyId { get; set; }
      public string Title { get; set; } = string.Empty;       // max 300, required
      public string? Notes { get; set; }
      public RecurrenceInterval Interval { get; set; }        // enum
      public int IntervalValue { get; set; }                  // e.g. 90 for "every 90 days"
      public DateOnly? LastCompletedDate { get; set; }        // denormalized for quick "next due" calc
      public bool IsActive { get; set; } = true;
      public DateTime CreatedAt { get; set; }
      public DateTime UpdatedAt { get; set; }
      public Property Property { get; set; } = null!;
      public ICollection<MaintenanceLog> Logs { get; set; } = [];
  }
  ```

- [ ] **Task F1.2** — Create `RecurrenceInterval` enum
  - **File:** `src/LifeWorks.Domain/Enums/RecurrenceInterval.cs` (new)
  ```csharp
  public enum RecurrenceInterval { Days, Weeks, Months, Years }
  ```

- [ ] **Task F1.3** — Create `MaintenanceLog` entity
  - **File:** `src/LifeWorks.Domain/Entities/MaintenanceLog.cs` (new)
  ```csharp
  public class MaintenanceLog
  {
      public Guid Id { get; set; }
      public Guid MaintenanceTaskId { get; set; }
      public DateOnly CompletedDate { get; set; }
      public decimal? Cost { get; set; }
      public string? Notes { get; set; }
      public DateTime CreatedAt { get; set; }
      public MaintenanceTask Task { get; set; } = null!;
  }
  ```

---

## Phase F2 — Infrastructure Layer

- [ ] **Task F2.1** — Register entities in `AppDbContext`
  - **File:** `src/LifeWorks.Infrastructure/Data/AppDbContext.cs`
  - Add: `public DbSet<MaintenanceTask> MaintenanceTasks => Set<MaintenanceTask>();`
  - Add: `public DbSet<MaintenanceLog> MaintenanceLogs => Set<MaintenanceLog>();`
  - Configure FK: `MaintenanceTask → Property` (OnDelete: Restrict)
  - Configure FK: `MaintenanceLog → MaintenanceTask` (OnDelete: Cascade)
  - Configure string lengths and decimal precision

- [ ] **Task F2.2** — Add EF Core migration
  - Run: `dotnet ef migrations add AddMaintenanceScheduler --project src/LifeWorks.Infrastructure --startup-project src/LifeWorks.Web`

---

## Phase F3 — Application Layer

- [ ] **Task F3.1** — Create `IMaintenanceTaskRepository`
  - **File:** `src/LifeWorks.Application/Repositories/IMaintenanceTaskRepository.cs` (new)
  ```csharp
  public interface IMaintenanceTaskRepository : IRepository<MaintenanceTask>
  {
      Task<List<MaintenanceTask>> GetByPropertyAsync(Guid? propertyId);
      Task<List<MaintenanceTask>> GetOverdueOrUpcomingAsync(int withinDays);
      Task<MaintenanceTask?> GetWithLogsAsync(Guid id);
  }
  ```

- [ ] **Task F3.2** — Create `IMaintenanceLogRepository`
  - **File:** `src/LifeWorks.Application/Repositories/IMaintenanceLogRepository.cs` (new)
  ```csharp
  public interface IMaintenanceLogRepository : IRepository<MaintenanceLog>
  {
      Task<List<MaintenanceLog>> GetByTaskAsync(Guid taskId);
  }
  ```

- [ ] **Task F3.3** — Implement `MaintenanceTaskRepository`
  - **File:** `src/LifeWorks.Infrastructure/Repositories/MaintenanceTaskRepository.cs` (new)
  - `GetOverdueOrUpcomingAsync`: compute `NextDueDate` from `LastCompletedDate + Interval` and return tasks due within `withinDays` days (or already overdue)
  - `GetWithLogsAsync`: include `Logs` navigation, ordered by CompletedDate descending

- [ ] **Task F3.4** — Implement `MaintenanceLogRepository`
  - **File:** `src/LifeWorks.Infrastructure/Repositories/MaintenanceLogRepository.cs` (new)
  - `GetByTaskAsync`: return logs for a task ordered by CompletedDate descending

- [ ] **Task F3.5** — Create `IMaintenanceService` and `MaintenanceService`
  - **File:** `src/LifeWorks.Application/Services/IMaintenanceService.cs` (new)
  - Methods:
    - `Task<List<MaintenanceTask>> GetAllTasksAsync(Guid? propertyId = null)`
    - `Task<MaintenanceTask?> GetTaskByIdAsync(Guid id)`
    - `Task AddTaskAsync(MaintenanceTask task)`
    - `Task UpdateTaskAsync(MaintenanceTask task)`
    - `Task DeleteTaskAsync(Guid id)`
    - `Task<List<MaintenanceTask>> GetOverdueOrUpcomingAsync(int withinDays = 30)`
    - `Task LogCompletionAsync(Guid taskId, DateOnly completedDate, decimal? cost, string? notes)`
    - `Task DeleteLogAsync(Guid logId)`
  - **File:** `src/LifeWorks.Application/Services/MaintenanceService.cs` (new)
  - `LogCompletionAsync`: creates a `MaintenanceLog`, updates `task.LastCompletedDate`, saves both; set timestamps

- [ ] **Task F3.6** — Register in DI
  - **File:** `src/LifeWorks.Infrastructure/DependencyInjection.cs`
  - Add `IMaintenanceTaskRepository`, `IMaintenanceLogRepository` → concrete implementations
  - **File:** `src/LifeWorks.Application/DependencyInjection.cs`
  - Add `IMaintenanceService` → `MaintenanceService`

---

## Phase F4 — Web Layer

- [ ] **Task F4.1** — Maintenance task list page
  - **File:** `src/LifeWorks.Web/Components/Pages/Maintenance/Index.razor` (new)
  - Route: `/maintenance`
  - Property filter (MudSelect)
  - MudDataGrid: Title, Property, Interval description (e.g. "Every 90 days"), Next Due Date, Last Completed, Status chip (Overdue/Due Soon/OK)
  - Color-code status: red = overdue, yellow = due within 14 days, green = OK
  - "Log Completion" action button per row (opens inline dialog)
  - Add/Edit/Delete task actions

- [ ] **Task F4.2** — Maintenance task form (add/edit)
  - **File:** `src/LifeWorks.Web/Components/Pages/Maintenance/MaintenanceTaskForm.razor` (new)
  - Routes: `/maintenance/new`, `/maintenance/{Id:guid}/edit`
  - Fields: Title (required), Property (required MudSelect), Interval value (MudNumericField) + Interval unit (MudSelect Days/Weeks/Months/Years), Notes, IsActive toggle
  - 2-column responsive grid

- [ ] **Task F4.3** — Log completion dialog
  - Inline `MudDialog` on the Index page
  - Fields: Completed Date (MudDatePicker, defaults to today), Cost (optional MudNumericField), Notes (optional MudTextField)
  - On submit: calls `MaintenanceService.LogCompletionAsync()`, reloads grid

- [ ] **Task F4.4** — Task detail / log history page
  - **File:** `src/LifeWorks.Web/Components/Pages/Maintenance/MaintenanceDetail.razor` (new)
  - Route: `/maintenance/{Id:guid}`
  - Shows task info card + full completion log history (date, cost, notes) with delete per entry

- [ ] **Task F4.5** — Dashboard widget for upcoming/overdue maintenance
  - **File:** `src/LifeWorks.Web/Components/Pages/Home.razor`
  - Add "Maintenance Due" section below existing widgets
  - Call `MaintenanceService.GetOverdueOrUpcomingAsync(30)`
  - Show: task title, property, next due date, status chip
  - Link each row to `/maintenance/{id}`

- [ ] **Task F4.6** — Add Maintenance to NavMenu
  - **File:** `src/LifeWorks.Web/Components/Layout/NavMenu.razor`
  - Add "Maintenance" link under the "Property Management" group

---

## Phase F5 — Tests

- [ ] **Task F5.1** — Unit tests for `MaintenanceService`
  - **File:** `tests/LifeWorks.Application.Tests/MaintenanceServiceTests.cs` (new)
  - Test: `AddTask_SetsTimestamps`
  - Test: `LogCompletion_UpdatesLastCompletedDate`
  - Test: `LogCompletion_CreatesLogEntry`
  - Test: `GetOverdueOrUpcoming_ReturnsTasksDueWithinWindow`
  - Test: `DeleteTask_WhenNotFound_DoesNotThrow`

---

## Verification

- [ ] `dotnet build` — zero errors/warnings
- [ ] `dotnet ef database update` — migration applies cleanly
- [ ] `dotnet test` — all tests pass
- [ ] Navigate to `/maintenance` — page loads with empty state
- [ ] Create a maintenance task (e.g. "Change HVAC filter, every 90 days") — saves correctly
- [ ] Log a completion — LastCompletedDate updates, log entry appears in history
- [ ] Task with overdue next-due-date shows red "Overdue" chip
- [ ] Dashboard shows the overdue task in "Maintenance Due" widget
- [ ] Delete a completion log entry — task history updates, LastCompletedDate recalculates if needed

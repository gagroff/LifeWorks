# LifeWorks — Phase 1 Implementation Plan

**Context:** The scaffolding is complete (domain entities, EF Core context, Blazor SSR shell, CI pipeline) but no features are implemented. This plan builds out all Phase 1 features from the PRD: Categories management, Contractors CRUD, and Home Improvements CRUD.

**How to use:** Work top-to-bottom. Check off each box as you complete it. Each phase ends with a Verification step — do not start the next phase until it passes. One PR per phase, targeting `main`, CI must pass before merge.

---

## Key Architecture Decisions

- **Repository pattern** — each entity gets an `IRepository<T>` interface in the Application layer; EF Core implementations live in Infrastructure. Services depend only on repository interfaces, not on `AppDbContext` directly.
- **Entity-first** — Blazor pages bind directly to domain entities for forms; introduce lightweight models only where projections are needed (e.g., `HomeImprovementFilter`).
- **`IsSeeded` flag on Category** — distinguishes seeded rows from user-created rows in the UI and service layer.
- **Audit timestamps set in service layer** — domain entities stay clean; services set `CreatedAt`/`UpdatedAt`.
- **Snackbar for success, inline validation for errors** — `MudSnackbar` for CUD confirmations; `EditForm` + `DataAnnotationsValidator` for field errors.

---

## Phase 1 — Infrastructure & Database Bootstrap

> **Goal:** Migrations created and applied; seed data loads on startup; app boots against LocalDB with a real schema.

### 1.1 — Add `IsSeeded` to Category Entity

- [x] Add `public bool IsSeeded { get; set; }` to `src/LifeWorks.Domain/Entities/Category.cs`

### 1.2 — Update DbContext

- [x] In `src/LifeWorks.Infrastructure/Data/AppDbContext.cs`, set `IsSeeded = true` on all 12 seeded `Category` objects in `OnModelCreating`

### 1.3 — Create Initial EF Core Migration

- [x] Run: `dotnet ef migrations add InitialCreate --project src/LifeWorks.Infrastructure --startup-project src/LifeWorks.Web`
- [x] Verify migration file created at `src/LifeWorks.Infrastructure/Migrations/` with `CREATE TABLE` for all 4 entities

### 1.4 — Apply Migration on Startup

- [x] In `src/LifeWorks.Web/Program.cs`, add a startup block after `var app = builder.Build()` that calls `context.Database.MigrateAsync()` using a scoped `AppDbContext`

### Phase 1 Verification

- [x] `dotnet build` — zero errors, zero warnings
- [ ] `dotnet run --project src/LifeWorks.Web` — app starts, no startup exceptions
- [ ] Database `LifeWorks` exists in LocalDB with 4 tables; `Categories` table has 12 rows with `IsSeeded = 1`
- [x] `dotnet test` — all tests pass
- [x] Commit on branch `feature/phase-1-infrastructure`, PR passes CI, merge

---

## Phase 2 — Repository & Application Service Layer

> **Goal:** Define repository interfaces in Application, implement them with EF Core in Infrastructure, build services on top, register everything in DI.

### 2.1 — Generic Repository Interface

- [x] Create `src/LifeWorks.Application/Repositories/IRepository.cs`:
  ```csharp
  Task<List<T>> GetAllAsync();
  Task<T?> GetByIdAsync(Guid id);
  Task AddAsync(T entity);
  Task UpdateAsync(T entity);
  Task DeleteAsync(T entity);
  Task SaveChangesAsync();
  ```

### 2.2 — Entity-Specific Repository Interfaces

- [x] Create `src/LifeWorks.Application/Repositories/ICategoryRepository.cs` extending `IRepository<Category>`:
  - `Task<List<Category>> GetAllOrderedAsync()` — ordered by SortOrder, then Name
  - `Task<bool> HasLinkedImprovementsAsync(Guid categoryId)`
- [x] Create `src/LifeWorks.Application/Repositories/IContractorRepository.cs` extending `IRepository<Contractor>`:
  - `Task<List<Contractor>> SearchAsync(string? searchTerm)`
  - `Task<int> GetLinkedImprovementCountAsync(Guid contractorId)`
- [x] Create `src/LifeWorks.Application/Repositories/IHomeImprovementRepository.cs` extending `IRepository<HomeImprovement>`:
  - `Task<List<HomeImprovement>> GetFilteredAsync(HomeImprovementFilter filter)`
  - `Task<HomeImprovement?> GetWithDetailsAsync(Guid id)` — includes Property, Category, Contractor
  - `Task<decimal> GetTotalCostAsync(HomeImprovementFilter filter)`
- [x] Create `src/LifeWorks.Application/Repositories/IPropertyRepository.cs` extending `IRepository<Property>`

### 2.3 — EF Core Repository Implementations (Infrastructure)

- [x] Create `src/LifeWorks.Infrastructure/Repositories/RepositoryBase.cs` — generic base implementing `IRepository<T>` using `AppDbContext`
- [x] Create `src/LifeWorks.Infrastructure/Repositories/CategoryRepository.cs` implementing `ICategoryRepository`
- [x] Create `src/LifeWorks.Infrastructure/Repositories/ContractorRepository.cs` implementing `IContractorRepository`:
  - `SearchAsync` — filters `Name.Contains(term)` OR `CompanyName.Contains(term)`
- [x] Create `src/LifeWorks.Infrastructure/Repositories/HomeImprovementRepository.cs` implementing `IHomeImprovementRepository`:
  - `GetFilteredAsync` — applies PropertyId, CategoryId, DateFrom, DateTo predicates; includes nav properties; `OrderByDescending(DateCompleted)`
  - `GetTotalCostAsync` — same filter predicates, sums `Cost` (null = 0)
- [x] Create `src/LifeWorks.Infrastructure/Repositories/PropertyRepository.cs` implementing `IPropertyRepository`

### 2.4 — Application Models

- [x] Create `src/LifeWorks.Application/Models/HomeImprovementFilter.cs` with `PropertyId?`, `CategoryId?`, `DateFrom?` (DateOnly), `DateTo?` (DateOnly)

### 2.5 — Application Services

- [x] Create `src/LifeWorks.Application/Services/ICategoryService.cs`:
  - `Task<List<Category>> GetAllAsync()`
  - `Task<Category?> GetByIdAsync(Guid id)`
  - `Task AddAsync(Category category)`
  - `Task UpdateAsync(Category category)`
  - `Task<bool> DeleteAsync(Guid id)` — returns `false` if seeded or in use
  - `Task<bool> CanDeleteAsync(Guid id)`
- [x] Create `src/LifeWorks.Application/Services/CategoryService.cs` — injects `ICategoryRepository`; `AddAsync` sets new `Guid` Id, `IsSeeded = false`
- [x] Create `src/LifeWorks.Application/Services/IContractorService.cs`:
  - `Task<List<Contractor>> GetAllAsync(string? searchTerm = null)`
  - `Task<Contractor?> GetByIdAsync(Guid id)`
  - `Task<int> GetLinkedImprovementCountAsync(Guid contractorId)`
  - `Task AddAsync(Contractor contractor)`
  - `Task UpdateAsync(Contractor contractor)`
  - `Task<bool> DeleteAsync(Guid id)` — returns `false` if linked improvements exist
- [x] Create `src/LifeWorks.Application/Services/ContractorService.cs` — injects `IContractorRepository`; `AddAsync`/`UpdateAsync` set `CreatedAt`/`UpdatedAt`
- [x] Create `src/LifeWorks.Application/Services/IHomeImprovementService.cs`:
  - `Task<List<HomeImprovement>> GetAllAsync(HomeImprovementFilter? filter = null)`
  - `Task<HomeImprovement?> GetByIdAsync(Guid id)`
  - `Task AddAsync(HomeImprovement improvement)`
  - `Task UpdateAsync(HomeImprovement improvement)`
  - `Task DeleteAsync(Guid id)`
  - `Task<decimal> GetTotalCostAsync(HomeImprovementFilter? filter = null)`
- [x] Create `src/LifeWorks.Application/Services/HomeImprovementService.cs` — injects `IHomeImprovementRepository`
- [x] Create `src/LifeWorks.Application/Services/IPropertyService.cs` with `Task<List<Property>> GetAllAsync()`
- [x] Create `src/LifeWorks.Application/Services/PropertyService.cs` — injects `IPropertyRepository`

### 2.6 — DI Registration

- [x] Create `src/LifeWorks.Infrastructure/DependencyInjection.cs` with `AddInfrastructure(this IServiceCollection services)` registering all 4 repository implementations as `Scoped`
- [x] Create `src/LifeWorks.Application/DependencyInjection.cs` with `AddApplicationServices(this IServiceCollection services)` registering all 4 services as `Scoped`
- [x] In `src/LifeWorks.Web/Program.cs`, call both: `builder.Services.AddInfrastructure()` and `builder.Services.AddApplicationServices()`

### Phase 2 Verification

- [x] `dotnet build` — zero errors
- [x] `dotnet test` — all tests pass
- [ ] Commit on branch `feature/phase-2-repositories-services`, PR passes CI, merge

---

## Phase 3 — Categories Feature

> **Goal:** Functional Categories settings page — list, add, edit, delete with guards.

### 3.1 — Global Imports

- [x] Add to `src/LifeWorks.Web/Components/_Imports.razor`:
  ```
  @using LifeWorks.Application.Services
  @using LifeWorks.Domain.Entities
  @using LifeWorks.Application.Models
  ```

### 3.2 — Categories List Page

- [x] Create `src/LifeWorks.Web/Components/Pages/Settings/Categories.razor` (`@page "/settings/categories"`)
- [x] Inject `ICategoryService` and `ISnackbar`
- [x] Load categories in `OnInitializedAsync`; show `MudProgressCircular` while loading
- [x] Render `MudTable` with columns: **Name**, **Sort Order**, **Seeded** (icon), **Actions**
- [x] Show empty-state message when no categories exist

### 3.3 — Add Category

- [x] "Add Category" `MudButton` above the table
- [x] `MudDialog` with **Name** (required, max 100) and **Sort Order** (required, int) fields
- [x] On submit: call `AddAsync`, refresh list, show snackbar "Category added"

### 3.4 — Edit Category

- [x] Edit icon button per row opens a dialog (same pattern as Add) pre-populated with current values
- [x] On save: call `UpdateAsync`, refresh list, show snackbar "Category updated"

### 3.5 — Delete Category

- [x] Delete button: disabled with tooltip for seeded categories; check `CanDeleteAsync` for in-use categories
- [x] `MudMessageBox` confirmation before deletion
- [x] On success: remove from list, show snackbar; on `false` return: show error snackbar

### Phase 3 Verification

- [ ] `/settings/categories` — 12 seeded categories appear; Delete disabled for all seeded rows
- [ ] Add a custom category — appears in list
- [ ] Edit a custom category — changes save correctly
- [ ] Delete custom category — succeeds; delete seeded category — blocked
- [x] `dotnet build` — zero errors
- [ ] Commit on branch `feature/phase-3-categories`, PR passes CI, merge

---

## Phase 4 — Contractors Feature

> **Goal:** Contractors list with search, Create/Edit form, detail view, delete with guard.

### 4.1 — Contractors List Page

- [x] Create `src/LifeWorks.Web/Components/Pages/Contractors/Index.razor` (`@page "/contractors"`)
- [x] Search `MudTextField` with debounce (`DebounceInterval="300"`) — reloads list via `GetAllAsync(searchTerm)`
- [x] `MudTable` columns: **Name**, **Company**, **Phone**, **Email**, **# Improvements**, **Actions** (View / Edit / Delete)
- [x] Empty-state message and loading spinner

### 4.2 — Contractor Create / Edit Form

- [x] Create `src/LifeWorks.Web/Components/Pages/Contractors/ContractorForm.razor` (`@page "/contractors/new"`, `@page "/contractors/{Id:guid}/edit"`)
- [x] In edit mode, load existing contractor via `GetByIdAsync(Id)`
- [x] Fields: Name (required), Company Name, Phone, Email, Website, Notes (multiline)
- [x] Submit: calls `AddAsync` or `UpdateAsync`, navigates to `/contractors` with snackbar
- [x] `EditForm` with `DataAnnotationsValidator`; add `[Required][MaxLength(200)]` to `Contractor.Name`

### 4.3 — Contractor Detail View

- [x] Create `src/LifeWorks.Web/Components/Pages/Contractors/ContractorDetail.razor` (`@page "/contractors/{Id:guid}"`)
- [x] `MudCard` read-only display of all fields; **Edit** and **Back** buttons

### 4.4 — Delete Contractor

- [x] Delete button on list: check `GetLinkedImprovementCountAsync`
  - [x] If > 0: show blocked message "Linked to {n} improvement record(s) — cannot delete"
  - [x] If 0: show `MudMessageBox` confirmation, then call `DeleteAsync`, refresh list, show snackbar

### Phase 4 Verification

- [ ] Create, edit, view, and delete a contractor
- [ ] Search filters list by name and company
- [ ] Delete blocked when contractor has linked improvements
- [x] `dotnet build` — zero errors
- [ ] Commit on branch `feature/phase-4-contractors`, PR passes CI, merge

---

## Phase 5 — Home Improvements Feature

> **Goal:** Full CRUD for the core feature — list with filters/sort/total, create/edit form, detail view, delete.

### 5.1 — Home Improvements List Page

- [ ] Create `src/LifeWorks.Web/Components/Pages/Improvements/Index.razor` (`@page "/improvements"`)
- [ ] Inject `IHomeImprovementService`, `IPropertyService`, `ICategoryService`
- [ ] Filter bar (`MudExpansionPanel` or always-visible):
  - `MudSelect<Guid?>` — Property
  - `MudSelect<Guid?>` — Category
  - Two `MudDatePicker` fields — Date range
  - **Apply** and **Clear** buttons
- [ ] `MudDataGrid<HomeImprovement>` columns: **Date** (default sort desc), **Title**, **Property**, **Category**, **Contractor** ("DIY" if null), **Cost** (currency format, blank if null), **Actions** (View / Edit / Delete)
- [ ] Running total below grid: `GetTotalCostAsync(currentFilter)` formatted as currency
- [ ] Empty-state message and loading spinner

### 5.2 — Create / Edit Form

- [ ] Create `src/LifeWorks.Web/Components/Pages/Improvements/ImprovementForm.razor` (`@page "/improvements/new"`, `@page "/improvements/{Id:guid}/edit"`)
- [ ] Inject `IHomeImprovementService`, `IPropertyService`, `ICategoryService`, `IContractorService`, `NavigationManager`, `ISnackbar`
- [ ] Fields (use `MudGrid`/`MudItem` for layout):
  - **Title** — required, `[Required][MaxLength(300)]`
  - **Property** — `MudSelect<Guid>`, required
  - **Category** — `MudSelect<Guid>`, required
  - **Date Completed** — `MudDatePicker`, required
  - **Cost** — `MudNumericField<decimal?>`, optional, "$" prefix
  - **Contractor** — `MudAutocomplete<Contractor?>` searching `IContractorService.GetAllAsync(term)`; clearable ("DIY / None")
  - **Warranty Expiration** — `MudDatePicker`, optional
  - **Detailed Notes** — `MudTextField` multiline, optional
  - **Manufacturer Info** — `MudExpansionPanel` (collapsed by default): Manufacturer Name, Model, Serial Number, Manufacturer Warranty Expiration
- [ ] Submit: `AddAsync` or `UpdateAsync`, navigate to `/improvements` with snackbar
- [ ] `EditForm` with `DataAnnotationsValidator`

### 5.3 — Detail View

- [ ] Create `src/LifeWorks.Web/Components/Pages/Improvements/ImprovementDetail.razor` (`@page "/improvements/{Id:guid}"`)
- [ ] `MudCard` read-only display of all fields
- [ ] Contractor section: if not null, show Name, Company, Phone, Email, Website (`MudLink` for email/website)
- [ ] Warranty chips: compute days remaining for `WarrantyExpiration` and `ManufacturerWarrantyExpiration`:
  - Green: > 90 days remaining
  - Yellow: 1–90 days remaining
  - Red: expired
- [ ] **Edit** and **Back to List** buttons

### 5.4 — Delete

- [ ] Delete button on list page: `MudMessageBox` confirmation "Delete '{title}'? This cannot be undone."
- [ ] On confirm: `DeleteAsync`, remove from local list, recalculate running total, show snackbar

### 5.5 — Navigation Wiring

- [ ] Verify `src/LifeWorks.Web/Components/Layout/NavMenu.razor` Href values match routes exactly: `"improvements"`, `"contractors"`, `"settings/categories"`

### Phase 5 Verification

- [ ] Create improvement with contractor + cost — appears in list
- [ ] Create DIY improvement (no contractor, no cost) — shows "DIY" and blank cost
- [ ] Filter by Property — list narrows, running total updates
- [ ] Filter by date range — narrows correctly
- [ ] Detail view: contractor contact info shown; warranty chips display correct color
- [ ] Edit: form pre-populates; changes save and reflect in list
- [ ] Delete: confirmation dialog; record removed; total updates
- [ ] `dotnet build` — zero errors
- [ ] Commit on branch `feature/phase-5-improvements`, PR passes CI, merge

---

## Phase 6 — Testing & Polish

> **Goal:** Replace placeholder tests with real unit tests; UI polish; final CI verification.

### 6.1 — Test Infrastructure

- [ ] Add `Microsoft.EntityFrameworkCore.InMemory` package to `tests/LifeWorks.Application.Tests/LifeWorks.Application.Tests.csproj`

### 6.2 — Category Service Tests

- [ ] Replace `tests/LifeWorks.Application.Tests/UnitTest1.cs` with `tests/LifeWorks.Application.Tests/CategoryServiceTests.cs`:
  - [ ] `GetAll_ReturnsOrderedBySortOrder`
  - [ ] `Add_AssignsNewGuidAndIsSeededFalse`
  - [ ] `Delete_SeededCategory_ReturnsFalse`
  - [ ] `Delete_CategoryInUse_ReturnsFalse`
  - [ ] `Delete_CustomUnusedCategory_Succeeds`

### 6.3 — Contractor Service Tests

- [ ] Create `tests/LifeWorks.Application.Tests/ContractorServiceTests.cs`:
  - [ ] `GetAll_WithSearchTerm_FiltersCorrectly`
  - [ ] `Delete_WithLinkedImprovement_ReturnsFalse`
  - [ ] `Delete_Unlinked_Succeeds`
  - [ ] `Add_SetsAuditTimestamps`

### 6.4 — HomeImprovement Service Tests

- [ ] Create `tests/LifeWorks.Application.Tests/HomeImprovementServiceTests.cs`:
  - [ ] `GetAll_WithPropertyFilter_ReturnsCorrectSubset`
  - [ ] `GetAll_WithDateRangeFilter_ReturnsCorrectSubset`
  - [ ] `GetTotalCost_SumsNonNullCosts_TreatsNullAsZero`
  - [ ] `GetTotalCost_WithFilter_RespectsFilter`
  - [ ] `Add_SetsAuditTimestamps`

### 6.5 — Domain Tests

- [ ] Replace `tests/LifeWorks.Domain.Tests/UnitTest1.cs` with `tests/LifeWorks.Domain.Tests/HomeImprovementTests.cs`:
  - [ ] `ContractorId_IsNullByDefault` (DIY baseline)
  - [ ] `Contractor_OptionalFields_AreNullByDefault`

### 6.6 — UI Polish

- [ ] Replace placeholder content in `src/LifeWorks.Web/Components/Pages/Home.razor` with a welcome `MudCard` and quick-link buttons to Improvements, Contractors, and Categories
- [ ] Confirm all list pages show a `MudProgressCircular` spinner during load and an empty-state message when no records exist

### 6.7 — Final CI Pass

- [ ] `dotnet build --configuration Release` — zero errors, zero warnings
- [ ] `dotnet test --configuration Release` — all tests pass (minimum 12 meaningful tests)
- [ ] Push branch, GitHub Actions CI green
- [ ] Commit on branch `feature/phase-6-testing`, PR passes CI, merge

---

## File Summary

| File | Action | Phase |
|---|---|---|
| `src/LifeWorks.Domain/Entities/Category.cs` | Modify — add `IsSeeded` | 1 |
| `src/LifeWorks.Infrastructure/Data/AppDbContext.cs` | Modify — `IsSeeded` on seed data | 1 |
| `src/LifeWorks.Infrastructure/Migrations/` | Create — initial migration | 1 |
| `src/LifeWorks.Web/Program.cs` | Modify — startup migration + DI registrations | 1 + 2 |
| `src/LifeWorks.Application/Repositories/IRepository.cs` | Create — generic interface | 2 |
| `src/LifeWorks.Application/Repositories/ICategoryRepository.cs` | Create | 2 |
| `src/LifeWorks.Application/Repositories/IContractorRepository.cs` | Create | 2 |
| `src/LifeWorks.Application/Repositories/IHomeImprovementRepository.cs` | Create | 2 |
| `src/LifeWorks.Application/Repositories/IPropertyRepository.cs` | Create | 2 |
| `src/LifeWorks.Infrastructure/Repositories/RepositoryBase.cs` | Create — EF Core base | 2 |
| `src/LifeWorks.Infrastructure/Repositories/CategoryRepository.cs` | Create | 2 |
| `src/LifeWorks.Infrastructure/Repositories/ContractorRepository.cs` | Create | 2 |
| `src/LifeWorks.Infrastructure/Repositories/HomeImprovementRepository.cs` | Create | 2 |
| `src/LifeWorks.Infrastructure/Repositories/PropertyRepository.cs` | Create | 2 |
| `src/LifeWorks.Infrastructure/DependencyInjection.cs` | Create | 2 |
| `src/LifeWorks.Application/DependencyInjection.cs` | Create | 2 |
| `src/LifeWorks.Application/Models/HomeImprovementFilter.cs` | Create | 2 |
| `src/LifeWorks.Application/Services/ICategoryService.cs` | Create | 2 |
| `src/LifeWorks.Application/Services/CategoryService.cs` | Create | 2 |
| `src/LifeWorks.Application/Services/IContractorService.cs` | Create | 2 |
| `src/LifeWorks.Application/Services/ContractorService.cs` | Create | 2 |
| `src/LifeWorks.Application/Services/IHomeImprovementService.cs` | Create | 2 |
| `src/LifeWorks.Application/Services/HomeImprovementService.cs` | Create | 2 |
| `src/LifeWorks.Application/Services/IPropertyService.cs` | Create | 2 |
| `src/LifeWorks.Application/Services/PropertyService.cs` | Create | 2 |
| `src/LifeWorks.Web/Components/_Imports.razor` | Modify — add Application/Domain usings | 3 |
| `src/LifeWorks.Web/Components/Pages/Settings/Categories.razor` | Create | 3 |
| `src/LifeWorks.Web/Components/Pages/Contractors/Index.razor` | Create | 4 |
| `src/LifeWorks.Web/Components/Pages/Contractors/ContractorForm.razor` | Create | 4 |
| `src/LifeWorks.Web/Components/Pages/Contractors/ContractorDetail.razor` | Create | 4 |
| `src/LifeWorks.Web/Components/Pages/Improvements/Index.razor` | Create | 5 |
| `src/LifeWorks.Web/Components/Pages/Improvements/ImprovementForm.razor` | Create | 5 |
| `src/LifeWorks.Web/Components/Pages/Improvements/ImprovementDetail.razor` | Create | 5 |
| `src/LifeWorks.Web/Components/Pages/Home.razor` | Modify — replace placeholder | 6 |
| `tests/LifeWorks.Application.Tests/LifeWorks.Application.Tests.csproj` | Modify — add EF InMemory package | 6 |
| `tests/LifeWorks.Application.Tests/CategoryServiceTests.cs` | Create (replaces UnitTest1) | 6 |
| `tests/LifeWorks.Application.Tests/ContractorServiceTests.cs` | Create | 6 |
| `tests/LifeWorks.Application.Tests/HomeImprovementServiceTests.cs` | Create | 6 |
| `tests/LifeWorks.Domain.Tests/HomeImprovementTests.cs` | Create (replaces UnitTest1) | 6 |

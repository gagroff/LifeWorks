# Plan B: Property Management UI (Full CRUD)

## Context
The `Property` entity, `PropertyService`, `IPropertyRepository`, and `PropertyRepository` all exist but are read-only. There is no Settings page to manage properties — users cannot add, rename, or delete properties. This plan wires up full CRUD end-to-end.

---

## Phase 1 — Application Layer: Add Write Methods

- [x] **Task B1.1** — Add `HasImprovementsAsync` to `IPropertyRepository`
  - **File:** `src/LifeWorks.Application/Repositories/IPropertyRepository.cs`
  - Note: `IRepository<Property>` base already provides `AddAsync`, `UpdateAsync`, `DeleteAsync`, `SaveChangesAsync` via `RepositoryBase`
  - Add:
    ```csharp
    Task<bool> HasImprovementsAsync(Guid propertyId);
    ```

- [x] **Task B1.2** — Implement `HasImprovementsAsync` in `PropertyRepository`
  - **File:** `src/LifeWorks.Infrastructure/Repositories/PropertyRepository.cs`
  - ```csharp
    public Task<bool> HasImprovementsAsync(Guid propertyId) =>
        Context.HomeImprovements.AnyAsync(h => h.PropertyId == propertyId);
    ```

- [x] **Task B1.3** — Extend `IPropertyService` with CRUD methods
  - **File:** `src/LifeWorks.Application/Services/IPropertyService.cs`
  - Add:
    ```csharp
    Task<Property?> GetByIdAsync(Guid id);
    Task AddAsync(Property property);
    Task UpdateAsync(Property property);
    Task<bool> DeleteAsync(Guid id); // returns false if property has improvements
    ```

- [x] **Task B1.4** — Implement CRUD in `PropertyService`
  - **File:** `src/LifeWorks.Application/Services/PropertyService.cs`
  - `AddAsync`: set `Id = Guid.NewGuid()`, `CreatedAt = UpdatedAt = DateTime.UtcNow`, call repo
  - `UpdateAsync`: set `UpdatedAt`, call repo
  - `DeleteAsync`: call `HasImprovementsAsync`; return `false` if true; otherwise delete and return `true`

---

## Phase 2 — Web Layer: Settings Pages

- [x] **Task B2.1** — Create `PropertyIndex.razor`
  - **File:** `src/LifeWorks.Web/Components/Pages/Settings/PropertyIndex.razor`
  - Route: `@page "/settings/properties"`
  - Inject `IPropertyService`
  - Display all properties in a `MudTable` with columns: Name, Address, Notes, Actions
  - Delete button: triggers confirmation dialog; shows error snackbar if property has linked improvements
  - "Add Property" button navigates to `/settings/properties/new`

- [x] **Task B2.2** — Create `PropertyForm.razor`
  - **File:** `src/LifeWorks.Web/Components/Pages/Settings/PropertyForm.razor`
  - Routes: `@page "/settings/properties/new"` and `@page "/settings/properties/{Id:guid}/edit"`
  - `MudTextField` for Name (required), Address (required), Notes (optional)
  - On submit: call `AddAsync` or `UpdateAsync` based on whether `Id` param is set
  - On success: navigate back to `/settings/properties`
  - Client-side validation via `EditForm` + `DataAnnotationsValidator`

- [x] **Task B2.3** — Add Properties link to NavMenu
  - **File:** `src/LifeWorks.Web/Components/Layout/NavMenu.razor`
  - Add inside the `Settings` `MudNavGroup`:
    ```xml
    <MudNavLink Href="settings/properties" Icon="@Icons.Material.Filled.House">
        Properties
    </MudNavLink>
    ```

---

## Phase 3 — Tests

- [x] **Task B3.1** — Unit tests for `PropertyService`
  - **File:** `tests/LifeWorks.Application.Tests/Services/PropertyServiceTests.cs`
  - `AddAsync` sets timestamps and new Guid
  - `DeleteAsync` returns `false` when property has improvements
  - `DeleteAsync` returns `true` and calls repo delete when no improvements

---

## Verification

- [x] `dotnet build` — no warnings
- [x] `dotnet test` — all tests pass (25 passed)
- [ ] `dotnet run --project src/LifeWorks.Web`:
  - [ ] Navigate to Settings > Properties — list renders
  - [ ] Add a new property — confirm it appears in the dropdown on the Improvements form
  - [ ] Edit a property name — confirm change persists
  - [ ] Attempt to delete a property with linked improvements — confirm error message
  - [ ] Delete a property with no improvements — confirm it's removed

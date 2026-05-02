# Plan A: Dashboard with Cost Analytics & Warranty Alerts

## Context
The Home page (`src/LifeWorks.Web/Components/Pages/Home.razor`) is a static welcome screen. All the data needed for a real dashboard already exists — costs on `HomeImprovement`, warranty dates, properties, and categories. No new migrations or entities are required.

---

## Phase 1 — Application Layer: Add Dashboard Query Methods

- [x] **Task A1.1** — Add new method signatures to `IHomeImprovementRepository`
  - **File:** `src/LifeWorks.Application/Repositories/IHomeImprovementRepository.cs`
  - Add:
    ```csharp
    Task<List<(string PropertyName, decimal TotalCost)>> GetCostByPropertyAsync();
    Task<List<(string CategoryName, decimal TotalCost)>> GetCostByCategoryAsync();
    Task<List<HomeImprovement>> GetExpiringWarrantiesAsync(int withinDays);
    Task<List<HomeImprovement>> GetRecentAsync(int count);
    ```

- [x] **Task A1.2** — Implement the new repository methods
  - **File:** `src/LifeWorks.Infrastructure/Repositories/HomeImprovementRepository.cs`
  - `GetCostByPropertyAsync` → group by `PropertyId`, select `Property.Name` + `Sum(Cost)`
  - `GetCostByCategoryAsync` → group by `CategoryId`, select `Category.Name` + `Sum(Cost)`
  - `GetExpiringWarrantiesAsync(withinDays)` → filter where `WarrantyExpiration` is between today and today+withinDays, order by soonest
  - `GetRecentAsync(count)` → `OrderByDescending(DateCompleted).Take(count)` with includes

- [x] **Task A1.3** — Add dashboard methods to `IHomeImprovementService`
  - **File:** `src/LifeWorks.Application/Services/IHomeImprovementService.cs`
  - Add matching interface methods mirroring the repository signatures above

- [x] **Task A1.4** — Implement dashboard methods in `HomeImprovementService`
  - **File:** `src/LifeWorks.Application/Services/HomeImprovementService.cs`
  - Delegate directly to repository — no business logic needed here

---

## Phase 2 — Web Layer: Build the Dashboard Page

- [x] **Task A2.1** — Rewrite `Home.razor` with dashboard layout
  - **File:** `src/LifeWorks.Web/Components/Pages/Home.razor`
  - Add `@inject IHomeImprovementService ImprovementService`
  - Add `OnInitializedAsync` to load all dashboard data
  - Add loading skeleton state while data loads

- [x] **Task A2.2** — Add Summary Stats row
  - Top row: Total improvements count, Total spend (all time), Active warranties count
  - Use `MudPaper`/`MudCard` with large prominent numbers

- [x] **Task A2.3** — Add Cost by Property cards
  - One `MudCard` per property showing property name + total spend
  - Uses `GetCostByPropertyAsync()`

- [x] **Task A2.4** — Add Cost by Category chart
  - Use MudBlazor's `MudChart` (bar chart) with category names on X axis and totals on Y axis
  - Uses `GetCostByCategoryAsync()`

- [x] **Task A2.5** — Add Warranty Alerts section
  - List improvements where `WarrantyExpiration` is within 90 days
  - Color-coded chips: green (>30d), warning (1–30d), red (expired)
  - Each row links to the improvement detail page
  - Uses `GetExpiringWarrantiesAsync(90)`

- [x] **Task A2.6** — Add Recent Activity feed
  - Last 5 improvements as a `MudList` with title, property, date, and cost
  - Uses `GetRecentAsync(5)`

---

## Phase 3 — Tests

- [x] **Task A3.1** — Unit tests for new service methods
  - **File:** `tests/LifeWorks.Application.Tests/Services/HomeImprovementServiceTests.cs`
  - Mock repository; assert service calls correct repo methods and returns expected shapes

---

## Verification

- [x] `dotnet build` — no warnings
- [x] `dotnet test` — all tests pass (20 passed)
- [ ] `dotnet run --project src/LifeWorks.Web` — visit `/`, confirm dashboard loads with real data, charts render, warranty alerts appear

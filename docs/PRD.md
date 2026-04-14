# LifeWorks — Product Requirements Document

**Version:** 1.0  
**Date:** 2026-04-14  
**Status:** Draft  

---

## 1. Overview

### 1.1 Purpose

LifeWorks is a personal property and life management hub. It replaces spreadsheets and scattered notes with a single, organized application for tracking the things that matter: home improvements, contractors, warranties, vehicles, travel, and more.

### 1.2 Properties Managed

| Property | Description |
|---|---|
| Primary Home | Main residence |
| Lake House | Secondary/vacation property |

### 1.3 Goals

- Replace the current spreadsheet used to track home repairs and improvements
- Provide a single source of truth for both properties
- Store contractor contact information so it is reusable across records
- Track warranties and manufacturer information for installed equipment
- Lay a foundation that can grow into a broader personal hub

### 1.4 Non-Goals (Phase 1)

- User authentication / multi-user support
- Mobile app
- Cloud deployment (designed for it, not required yet)
- File attachments (planned for Phase 2)
- Reporting dashboards (planned for a later phase)

---

## 2. Tech Stack

| Layer | Technology |
|---|---|
| Framework | .NET 10, Blazor SSR |
| UI Components | MudBlazor |
| Data Access | EF Core 10 |
| Database | SQL Server (LocalDB for development) |
| Architecture | Clean Architecture (Domain / Application / Infrastructure / Web) |
| Testing | xUnit |
| Source Control | GitHub |
| CI/CD | GitHub Actions |
| Static Analysis | Roslyn Analyzers |
| Deployment Target | Local (Azure-ready) |

---

## 3. Phase 1 — Home Improvements Tracker

### 3.1 Data Model

#### Property
Represents a physical property owned by the user.

| Field | Type | Notes |
|---|---|---|
| Id | Guid | Primary key |
| Name | string | e.g., "Primary Home", "Lake House" |
| Address | string | Full address |
| Notes | string? | Optional free-text notes |
| CreatedAt | DateTime | Audit — set on insert |
| UpdatedAt | DateTime | Audit — set on update |

**Seed data:** Primary Home, Lake House

---

#### Category
Classifies the type of home improvement or repair.

| Field | Type | Notes |
|---|---|---|
| Id | Guid | Primary key |
| Name | string | e.g., "Plumbing", "HVAC" |
| SortOrder | int | Controls display order |

**Seed data:**
- Appliances
- Electrical
- Exterior / Siding
- Flooring
- HVAC
- Landscaping / Outdoor
- Painting
- Plumbing
- Roofing
- Structural
- Windows & Doors
- Other

---

#### Contractor
A reusable record for a service provider or vendor.

| Field | Type | Notes |
|---|---|---|
| Id | Guid | Primary key |
| Name | string | Contact person's name |
| CompanyName | string? | Business name |
| Phone | string? | Primary phone |
| Email | string? | Email address |
| Website | string? | Website URL |
| Notes | string? | Free-text notes |
| CreatedAt | DateTime | Audit |
| UpdatedAt | DateTime | Audit |

---

#### HomeImprovement
The core record of a repair, improvement, or maintenance event.

| Field | Type | Notes |
|---|---|---|
| Id | Guid | Primary key |
| PropertyId | Guid | FK → Property |
| CategoryId | Guid | FK → Category |
| Title | string | Short description (e.g., "Replaced water heater") |
| DetailedNotes | string? | Full description, observations, etc. |
| DateCompleted | DateOnly | When the work was completed |
| Cost | decimal? | Total cost (labor + materials) |
| ContractorId | Guid? | FK → Contractor (null = DIY) |
| WarrantyExpiration | DateOnly? | Labor/service warranty expiration |
| ManufacturerName | string? | e.g., "Rheem" |
| ManufacturerModel | string? | Model number |
| ManufacturerSerialNumber | string? | Serial number |
| ManufacturerWarrantyExpiration | DateOnly? | Product warranty expiration |
| CreatedAt | DateTime | Audit |
| UpdatedAt | DateTime | Audit |

---

### 3.2 Features

#### Home Improvements — List View
- Displays all improvement records in a MudBlazor data grid
- Filter by: Property, Category, date range
- Sort by: Date, Cost, Title, Property, Category
- Show running cost total for the current filter
- Quick-access buttons: View, Edit, Delete

#### Home Improvements — Create / Edit
- Form with all fields from the data model
- Contractor field is a searchable dropdown (reuse existing or add new inline)
- Category is a dropdown
- Property is a dropdown
- Cost is optional (DIY work may have no tracked cost)
- ContractorId is optional (null = DIY)
- Validation: Title and DateCompleted are required

#### Home Improvements — Detail View
- Read-only display of all fields
- Shows linked contractor's full contact info
- Highlights upcoming or expired warranties

#### Home Improvements — Delete
- Confirmation dialog before deletion
- Soft delete not required in Phase 1

---

#### Contractors — List View
- Displays all contractor records
- Search by name or company
- Quick-access: View, Edit, Delete

#### Contractors — Create / Edit
- Form with all contractor fields
- Shows count of linked improvement records

#### Contractors — Delete
- Blocked if the contractor is linked to any improvement records (show message)

---

#### Categories — Management
- List of categories with name and sort order
- Ability to add custom categories
- Predefined seed categories cannot be deleted (soft-lock)
- Custom categories can be deleted if not in use

---

### 3.3 Navigation

```
LifeWorks
├── Home (dashboard placeholder)
├── Home Improvements
│   ├── List
│   ├── Add New
│   └── [record] → Detail / Edit
├── Contractors
│   ├── List
│   └── Add New
└── Settings
    └── Categories
```

---

## 4. Future Phases

| Phase | Feature | Notes |
|---|---|---|
| 2 | File Attachments | Attach receipts, photos, PDFs to improvement records. Storage: local filesystem first, Azure Blob later. |
| 3 | Auto / Vehicle Records | Track service history per vehicle. OCR of service documents to auto-populate fields. |
| 4 | TODO List | Simple task list, possibly property-scoped. |
| 5 | Grocery List | Shopping list with categories. |
| 6 | US Travel Map | Interactive map of visited locations. |
| 7 | Restaurant List | Personal restaurant reviews and ratings. |
| 8 | Reporting | Cost summaries by property, category, year. Warranty expiration alerts. |
| N | Azure Deployment | Azure App Service + Azure SQL. No auth changes needed if single-user. |

---

## 5. Development Workflow

### 5.1 Repository
- GitHub: `gagroff/LifeWorks`
- Default branch: `main` (protected)
- Branch naming: `feature/<short-description>`, `fix/<short-description>`

### 5.2 Pull Request Workflow
1. Create a feature branch from `main`
2. Commit work to the feature branch
3. Open a Pull Request targeting `main`
4. CI must pass before merge
5. Merge via squash commit

### 5.3 CI/CD (GitHub Actions)
- Trigger: push to any branch, PR to `main`
- Steps: restore → build → test
- Build treats warnings as errors (`-warnaserror`)
- All xUnit tests must pass

### 5.4 Static Analysis
- Roslyn analyzers enabled in all projects
- Additional ruleset TBD (SonarCloud consideration for a later phase)

### 5.5 AI Assistance
- **GitHub Copilot agents:** Small, well-scoped issues (renaming, formatting, simple bug fixes)
- **Claude Code agents:** Multi-step feature implementation, architecture decisions, scaffolding

---

## 6. Non-Functional Requirements

| Requirement | Detail |
|---|---|
| Platform | Windows, runs locally via Kestrel |
| Database | SQL Server (LocalDB for dev, full SQL Server for prod) |
| Azure readiness | Designed to migrate to Azure App Service + Azure SQL with minimal changes |
| Authentication | None in Phase 1 |
| Accessibility | MudBlazor defaults; no specific WCAG target in Phase 1 |
| Browser support | Modern browsers (Edge, Chrome, Firefox) |

---

## 7. Open Questions

| # | Question | Status |
|---|---|---|
| 1 | Should cost fields track currency (USD assumed)? | Open |
| 2 | Should Properties be user-editable or fixed (only 2 properties initially)? | Open — seed + allow add |
| 3 | SonarCloud integration — Phase 1 or later? | Deferred |

# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

LifeWorks is a personal property and home improvement management hub built with .NET 10, Blazor SSR, and SQL Server. It uses Clean Architecture with four layers: Domain, Application, Infrastructure, and Web.

## Common Commands

```bash
# Build
dotnet build

# Run the web app
dotnet run --project src/LifeWorks.Web

# Run all tests
dotnet test

# Run a single test project
dotnet test tests/LifeWorks.Application.Tests
dotnet test tests/LifeWorks.Domain.Tests

# EF Core migrations
dotnet ef migrations add <MigrationName> --project src/LifeWorks.Infrastructure --startup-project src/LifeWorks.Web
dotnet ef database update --project src/LifeWorks.Infrastructure --startup-project src/LifeWorks.Web
```

## Architecture

**Layer responsibilities:**
- `LifeWorks.Domain` — Pure entities (Category, Contractor, HomeImprovement, Property). No dependencies.
- `LifeWorks.Application` — Service interfaces/implementations, repository interfaces, business logic. Timestamps (CreatedAt/UpdatedAt) are set here, not in Domain.
- `LifeWorks.Infrastructure` — EF Core `AppDbContext`, repository implementations, migrations.
- `LifeWorks.Web` — Blazor SSR pages and components using MudBlazor. DI is configured in `Program.cs` via `AddInfrastructure()` and `AddApplicationServices()` extension methods.

**Data flow:**
```
Blazor Pages → Application Services → Repository Interfaces → Infrastructure Repositories → SQL Server
```

**Key patterns:**
- Repository pattern with entity-specific interfaces (`ICategoryRepository`, `IContractorRepository`, etc.)
- Services depend only on repository interfaces, never on Infrastructure directly
- `IsSeeded` flag on Category distinguishes system seed data from user-created entries
- `HomeImprovementFilter` is used for filtered queries/projections — not a domain model

## Tech Stack

| Concern | Technology |
|---|---|
| Framework | .NET 10, Blazor SSR |
| UI Components | MudBlazor 9.3.0 |
| ORM | Entity Framework Core 10 |
| Database | SQL Server (LocalDB for dev) |
| Tests | xUnit (unit), Playwright (E2E) |

## Build Configuration

`Directory.Build.props` enables warnings-as-errors and latest Roslyn analysis across all projects. C# 14 with nullable enabled and implicit usings.

## Implementation Plans

All implementation plans live in `docs/ImplementationPlans/` and follow the naming convention `Plan[Letter]-[FeatureName].md` (e.g. `PlanA-Dashboard.md`). When creating new plans, always write them to this folder — not to the Claude-managed plans folder.

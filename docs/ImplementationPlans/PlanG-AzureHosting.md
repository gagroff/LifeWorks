# Plan G — Azure Hosting with Authentication

## Goal
Host LifeWorks on Azure App Service (Free F1) with Azure SQL Serverless and ASP.NET Core Identity authentication. Total target cost: **~$5–10/month**. Accessible anywhere via HTTPS at `https://lifeworks-<suffix>.azurewebsites.net`, including mobile browsers.

## Architecture
- **Web:** Azure App Service (Linux, Free F1) running .NET 10 Blazor SSR.
- **Database:** Azure SQL Database, Serverless tier (0.5 vCore min, auto-pause 1hr, 2 GB).
- **Auth:** ASP.NET Core Identity using `IdentityUser`, persisted to `AppDbContext`. Public registration disabled; two accounts seeded from environment variables.
- **Storage:** Azure Blob Storage (Hot tier, LRS) for Data Protection keys and attachment files. Replaces local `wwwroot/uploads`.
- **Secrets:** All connection strings + admin credentials read from App Service Configuration (env vars). Nothing sensitive committed.
- **Deployment:** GitHub Actions workflow on push to `main`. EF migrations run automatically on app startup (already wired in `Program.cs`).

## Helpful Plugins / Skills
Use these during execution (not all at once):
- **`superpowers:subagent-driven-development`** or **`superpowers:executing-plans`** — to work through the phases.
- **`superpowers:test-driven-development`** — for Phase G1 (auth) and Phase G2 (storage abstraction). Tests first.
- **`frontend-design:frontend-design`** — when building the login page in Task G1.6 to keep it visually consistent with MudBlazor / non-generic.
- **`superpowers:verification-before-completion`** — before checking off Phase G7 verification items. Evidence before assertions.
- **`code-simplifier:simplify`** — after Phase G2 and Phase G3 to catch redundant abstractions before they ship.
- **`context7`** (MCP) — when wiring up Azure SDK packages (`Azure.Storage.Blobs`, `Azure.Identity`, `Microsoft.AspNetCore.DataProtection.AzureStorage`); fetches current docs so we don't guess outdated API shapes.

---

# Phase G1 — Authentication (locally testable)

Goal: Add ASP.NET Core Identity, gate all pages behind login, seed two accounts. Verifiable entirely on LocalDB before any Azure work.

- [ ] **Task G1.1** — Add Identity NuGet packages
  - **File:** `src/LifeWorks.Infrastructure/LifeWorks.Infrastructure.csproj`
  - Add: `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (v10.x, match EF Core 10)
  - **File:** `src/LifeWorks.Web/LifeWorks.Web.csproj`
  - Add: `Microsoft.AspNetCore.Identity.UI` (only if we want default email/password UI helpers — likely not, we'll build our own)
  - Add: `Microsoft.AspNetCore.Components.Authorization`

- [ ] **Task G1.2** — Make `AppDbContext` inherit from `IdentityDbContext`
  - **File:** `src/LifeWorks.Infrastructure/Data/AppDbContext.cs`
  - Change base class from `DbContext` to `IdentityDbContext<IdentityUser>`
  - In `OnModelCreating`, call `base.OnModelCreating(builder)` first so Identity tables are configured
  - No custom user properties yet — `IdentityUser` is enough for two known accounts

- [ ] **Task G1.3** — Add EF Core migration for Identity schema
  - Run: `dotnet ef migrations add AddIdentity --project src/LifeWorks.Infrastructure --startup-project src/LifeWorks.Web`
  - Verify migration adds `AspNetUsers`, `AspNetRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserRoles`, `AspNetUserTokens`, `AspNetRoleClaims`

- [ ] **Task G1.4** — Wire Identity services in `Program.cs`
  - **File:** `src/LifeWorks.Web/Program.cs`
  - After `AddDbContext`, add:
    ```csharp
    builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
        {
            options.Password.RequiredLength = 12;
            options.Password.RequireNonAlphanumeric = true;
            options.SignIn.RequireConfirmedAccount = false;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

    builder.Services.AddCascadingAuthenticationState();
    builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
    ```
  - After `app.UseHttpsRedirection()` add: `app.UseAuthentication(); app.UseAuthorization();`

- [ ] **Task G1.5** — Add `IdentityRevalidatingAuthenticationStateProvider`
  - **File:** `src/LifeWorks.Web/Components/Account/IdentityRevalidatingAuthenticationStateProvider.cs` (new)
  - Copy from the standard Blazor Server + Identity template: revalidates user every 30 min, signs out if user is deleted/locked
  - Pattern: `RevalidatingServerAuthenticationStateProvider` subclass calling `UserManager<IdentityUser>.GetUserAsync(principal)`

- [ ] **Task G1.6** — Create login page (Blazor SSR component, MudBlazor styling)
  - **File:** `src/LifeWorks.Web/Components/Account/Login.razor` (new)
  - Route: `/login`, `@page` directive with `[AllowAnonymous]`
  - `EditForm` with `Email`, `Password`, `RememberMe`
  - On submit, call `SignInManager<IdentityUser>.PasswordSignInAsync`
  - On success, redirect to return URL or `/`
  - On failure, show MudAlert with "Invalid credentials"
  - **Design note:** When implementing, invoke `frontend-design:frontend-design` for a non-generic auth page that fits the MudBlazor aesthetic.

- [ ] **Task G1.7** — Add logout endpoint
  - **File:** `src/LifeWorks.Web/Program.cs`
  - Add minimal API endpoint:
    ```csharp
    app.MapPost("/logout", async (SignInManager<IdentityUser> signInManager) =>
    {
        await signInManager.SignOutAsync();
        return Results.Redirect("/login");
    });
    ```

- [ ] **Task G1.8** — Add logout button to layout
  - **File:** `src/LifeWorks.Web/Components/Layout/MainLayout.razor` (or `NavMenu.razor`)
  - Add `<AuthorizeView>` block showing user email + logout form (POST to `/logout` with antiforgery token)

- [ ] **Task G1.9** — Globally require authorization on all routes
  - **File:** `src/LifeWorks.Web/Components/Routes.razor` (or `App.razor`)
  - Wrap `RouteView` with `AuthorizeRouteView` and set `DefaultLayout`
  - In `_Imports.razor`, add `@attribute [Authorize]` so all pages require login by default
  - Explicitly mark `Login.razor` with `[AllowAnonymous]`

- [ ] **Task G1.10** — Seed two accounts at startup from configuration
  - **File:** `src/LifeWorks.Web/Program.cs`
  - In the existing startup `using` block, after `MigrateAsync()`, call a new `IdentitySeeder.SeedAsync(scope)` helper
  - **File:** `src/LifeWorks.Web/IdentitySeeder.cs` (new)
  - Reads `Identity:SeedUsers` config section (array of `{Email, Password}`). Creates each via `UserManager.CreateAsync` if not present
  - **File:** `src/LifeWorks.Web/appsettings.Development.json` (new)
  - Add a dev-only `Identity:SeedUsers` block with your local dev credentials (file already in `.gitignore` if it exists — verify and add if not)

- [ ] **Task G1.11** — Disable public registration
  - There is no registration page being added in this plan. If `Microsoft.AspNetCore.Identity.UI` was included in G1.1, exclude its registration endpoint via `MapIdentityApi` exclusion, or simply do not call any UI-registration mapping. Confirm `/register` returns 404.

- [ ] **Task G1.12** — Unit tests for seeder
  - **File:** `tests/LifeWorks.Application.Tests/IdentitySeederTests.cs` (new)
  - Test: `Seed_WithTwoUsers_CreatesBothInDatabase`
  - Test: `Seed_WhenUserAlreadyExists_DoesNotDuplicate`
  - Test: `Seed_WithEmptyConfig_DoesNothing`
  - Use in-memory SQLite or EF in-memory provider for `UserManager`

- [ ] **Task G1.13** — Manual smoke test
  - Run: `dotnet run --project src/LifeWorks.Web`
  - Verify: visiting `/` while not logged in redirects to `/login`
  - Verify: logging in with seeded credentials lands on dashboard
  - Verify: logout button returns to login page
  - Verify: hitting `/register` returns 404

**Phase G1 commit:** `feat(auth): add ASP.NET Identity with seeded accounts and login flow`

---

# Phase G2 — Storage Abstraction for Attachments

Goal: Replace direct filesystem usage in `AttachmentService` with an `IFileStorage` interface so we can swap to Blob Storage in Phase G3. Keep working locally with a filesystem implementation.

- [ ] **Task G2.1** — Define `IFileStorage` interface
  - **File:** `src/LifeWorks.Application/Storage/IFileStorage.cs` (new)
  - Methods:
    ```csharp
    Task SaveAsync(string storedName, Stream content, string contentType, CancellationToken ct = default);
    Task<Stream> OpenReadAsync(string storedName, CancellationToken ct = default);
    Task DeleteAsync(string storedName, CancellationToken ct = default);
    ```

- [ ] **Task G2.2** — Implement `LocalFileStorage`
  - **File:** `src/LifeWorks.Infrastructure/Storage/LocalFileStorage.cs` (new)
  - Constructor takes `IConfiguration`, reads `Attachments:UploadPath`
  - `SaveAsync`: create dir, write stream to `Path.Combine(root, storedName)`
  - `OpenReadAsync`: return `File.OpenRead(...)`, throw `FileNotFoundException` if missing
  - `DeleteAsync`: delete file if exists; do nothing if not

- [ ] **Task G2.3** — Refactor `AttachmentService` to depend on `IFileStorage`
  - **File:** `src/LifeWorks.Application/Services/AttachmentService.cs`
  - Remove `IConfiguration` dependency, remove `UploadPath` property, remove all `File.*` and `Directory.*` calls
  - Inject `IFileStorage` instead
  - `SaveAsync`: compute `storedFileName`, call `fileStorage.SaveAsync(storedFileName, fileStream, contentType)`
  - `GetFileAsync`: call `fileStorage.OpenReadAsync(attachment.StoredFileName)`
  - `DeleteAsync`: call `fileStorage.DeleteAsync(attachment.StoredFileName)` before deleting metadata
  - Note: `Attachment.FileSize` was previously computed from `new FileInfo(fullPath).Length`. Switch to tracking bytes copied during stream copy, or set after write via a `CountingStream` wrapper. Update the test in G2.6 accordingly.

- [ ] **Task G2.4** — Register `LocalFileStorage` in DI
  - **File:** `src/LifeWorks.Infrastructure/InfrastructureServiceCollectionExtensions.cs` (or wherever `AddInfrastructure` lives)
  - Add: `services.AddSingleton<IFileStorage, LocalFileStorage>();`

- [ ] **Task G2.5** — Run existing attachment tests
  - Run: `dotnet test tests/LifeWorks.Application.Tests --filter Attachment`
  - Expected: pass after refactor. Fix any breakages from the `IConfiguration` removal.

- [ ] **Task G2.6** — Add unit tests for `LocalFileStorage`
  - **File:** `tests/LifeWorks.Application.Tests/LocalFileStorageTests.cs` (new)
  - Use a temp directory (`Path.GetTempPath()` + Guid subfolder, cleanup in `IDisposable.Dispose`)
  - Tests: `Save_ThenOpenRead_ReturnsSameBytes`, `Delete_RemovesFile`, `Delete_OnMissingFile_DoesNotThrow`, `OpenRead_OnMissingFile_Throws`

- [ ] **Task G2.7** — Manual smoke test
  - Run app, upload an attachment to a HomeImprovement, download it, delete it
  - Verify file appears/disappears in `wwwroot/uploads`

**Phase G2 commit:** `refactor(attachments): introduce IFileStorage abstraction`

---

# Phase G3 — Azure Adapters (Blob Storage + Data Protection)

Goal: Add the Azure-specific implementations. Switchable via configuration. Still runs locally with `LocalFileStorage` if Azure config absent.

- [ ] **Task G3.1** — Add Azure NuGet packages
  - **File:** `src/LifeWorks.Infrastructure/LifeWorks.Infrastructure.csproj`
  - Add: `Azure.Storage.Blobs` (latest stable)
  - Add: `Azure.Identity` (latest stable)
  - **File:** `src/LifeWorks.Web/LifeWorks.Web.csproj`
  - Add: `Microsoft.AspNetCore.DataProtection.AzureStorage` (latest)
  - Add: `Azure.Identity`

- [ ] **Task G3.2** — Implement `BlobFileStorage`
  - **File:** `src/LifeWorks.Infrastructure/Storage/BlobFileStorage.cs` (new)
  - Constructor takes `BlobServiceClient` and reads container name from config (`Storage:AttachmentsContainer`, default `attachments`)
  - Ensures container exists on first use (`CreateIfNotExistsAsync`, private access)
  - `SaveAsync`: `BlobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType }, ...)`
  - `OpenReadAsync`: `BlobClient.OpenReadAsync()`. If not found, throw `FileNotFoundException` to match `LocalFileStorage` contract
  - `DeleteAsync`: `BlobClient.DeleteIfExistsAsync()`

- [ ] **Task G3.3** — Conditional DI registration based on configuration
  - **File:** `src/LifeWorks.Infrastructure/InfrastructureServiceCollectionExtensions.cs`
  - Change `AddInfrastructure` to accept `IConfiguration`
  - If `Storage:BlobConnectionString` is present (or `Storage:BlobServiceUri` for managed identity), register `BlobServiceClient` and `BlobFileStorage`
  - Otherwise register `LocalFileStorage`
  - **File:** `src/LifeWorks.Web/Program.cs`
  - Update call site to pass `builder.Configuration`

- [ ] **Task G3.4** — Configure Data Protection to persist keys to Blob
  - **File:** `src/LifeWorks.Web/Program.cs`
  - After Identity registration, add:
    ```csharp
    var blobConn = builder.Configuration["Storage:BlobConnectionString"];
    if (!string.IsNullOrEmpty(blobConn))
    {
        builder.Services.AddDataProtection()
            .PersistKeysToAzureBlobStorage(blobConn, "data-protection", "keys.xml")
            .SetApplicationName("LifeWorks");
    }
    ```
  - When `blobConn` is absent (local dev), Data Protection uses its default local key ring — fine for dev.

- [ ] **Task G3.5** — Connection string from environment in production
  - **File:** `src/LifeWorks.Web/Program.cs`
  - No code change needed — `builder.Configuration.GetConnectionString("DefaultConnection")` already reads from env vars (`ConnectionStrings__DefaultConnection` will override appsettings.json in App Service)
  - Confirm `appsettings.json` retains the LocalDB string as fallback (already does — verified in current file)

- [ ] **Task G3.6** — Add `Microsoft.AspNetCore.HttpsPolicy` HSTS configuration
  - **File:** `src/LifeWorks.Web/Program.cs`
  - HSTS is already enabled in production via `app.UseHsts()`. Confirm default 30-day max-age is acceptable, or set:
    ```csharp
    builder.Services.AddHsts(o => { o.MaxAge = TimeSpan.FromDays(365); o.IncludeSubDomains = true; });
    ```

- [ ] **Task G3.7** — Tests for `BlobFileStorage` (integration, optional)
  - **File:** `tests/LifeWorks.Application.Tests/BlobFileStorageTests.cs` (new)
  - Skip these in CI unless `BLOB_TEST_CONNECTION_STRING` env var is set (use `Skip.If`)
  - Same test shape as `LocalFileStorageTests` — proves contract equivalence
  - Alternative: use Azurite (Azure Storage emulator) — document in test file how to spin it up locally

**Phase G3 commit:** `feat(storage): add Azure Blob storage adapter and Data Protection key persistence`

---

# Phase G4 — Azure Provisioning

Goal: Create the Azure resources via Azure CLI. All commands idempotent enough to re-run. No code changes in this phase.

> Before starting: install Azure CLI (`az --version` should work). Run `az login` once.

- [x] **Task G4.1** — Choose a unique suffix and region
  - Pick a short suffix (e.g. your initials + 3 digits): `ggr042`
  - Pick a region close to you: `eastus`, `westus2`, `centralus` etc.
  - Record these — they're used in every subsequent command
  - All resource names below use `$SUFFIX` as a placeholder

- [x] **Task G4.2** — Create resource group
  - Run:
    ```bash
    az group create --name lifeworks-rg --location eastus
    ```

- [x] **Task G4.3** — Create App Service plan (Free F1, Linux)
  - Run:
    ```bash
    az appservice plan create --name lifeworks-plan --resource-group lifeworks-rg --sku F1 --is-linux
    ```
  - Note: F1 is per-subscription-limited (10 free apps). If you have other F1 apps you may need B1 (~$13/mo).

- [x] **Task G4.4** — Create Web App
  - Run:
    ```bash
    az webapp create --name lifeworks-$SUFFIX --resource-group lifeworks-rg --plan lifeworks-plan --runtime "DOTNETCORE:10.0"
    ```
  - URL will be `https://lifeworks-$SUFFIX.azurewebsites.net`
  - Note: at time of writing .NET 10 may need `DOTNETCORE:10.0` or may require setting via `az webapp config set --linux-fx-version`. If `az webapp create` rejects, omit `--runtime` and set after with `az webapp config set --linux-fx-version "DOTNETCORE|10.0"`.

- [x] **Task G4.5** — Enable HTTPS-only and disable FTPS
  - Run:
    ```bash
    az webapp update --name lifeworks-$SUFFIX --resource-group lifeworks-rg --https-only true
    az webapp config set --name lifeworks-$SUFFIX --resource-group lifeworks-rg --ftps-state Disabled
    ```

- [x] **Task G4.6** — Create Azure SQL Server
  - Pick a strong admin password (save to a password manager — needed for connection string)
  - Run:
    ```bash
    az sql server create --name lifeworks-sql-$SUFFIX --resource-group lifeworks-rg --location eastus --admin-user lwadmin --admin-password "<STRONG_PASSWORD>"
    ```

- [x] **Task G4.7** — Allow Azure services to access the SQL Server
  - Run:
    ```bash
    az sql server firewall-rule create --resource-group lifeworks-rg --server lifeworks-sql-$SUFFIX --name AllowAzureServices --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0
    ```
  - The 0.0.0.0/0.0.0.0 rule means "Azure services only" (special semantics — NOT all IPs)
  - Optionally add your home IP for direct DB access from SSMS/Azure Data Studio:
    ```bash
    az sql server firewall-rule create --resource-group lifeworks-rg --server lifeworks-sql-$SUFFIX --name HomeIP --start-ip-address <YOUR_IP> --end-ip-address <YOUR_IP>
    ```

- [x] **Task G4.8** — Create Serverless SQL Database
  - Run:
    ```bash
    az sql db create --resource-group lifeworks-rg --server lifeworks-sql-$SUFFIX --name LifeWorks --edition GeneralPurpose --compute-model Serverless --family Gen5 --min-capacity 0.5 --capacity 2 --max-size 2GB --auto-pause-delay 60 --backup-storage-redundancy Local
    ```
  - `--auto-pause-delay 60` = pause after 60 minutes idle (lowest cost)
  - `--backup-storage-redundancy Local` = cheapest backup option (LRS), 7-day PITR retention default

- [x] **Task G4.9** — Create Storage Account
  - Run:
    ```bash
    az storage account create --name lifeworksst$SUFFIX --resource-group lifeworks-rg --location eastus --sku Standard_LRS --kind StorageV2 --access-tier Hot --allow-blob-public-access false
    ```
  - Storage account names: 3–24 chars, lowercase + digits only

- [x] **Task G4.10** — Get connection strings
  - Run (record both outputs):
    ```bash
    az sql db show-connection-string --client ado.net --server lifeworks-sql-$SUFFIX --name LifeWorks
    az storage account show-connection-string --name lifeworksst$SUFFIX --resource-group lifeworks-rg --query connectionString -o tsv
    ```
  - Replace `<username>` and `<password>` in the SQL string with `lwadmin` and your admin password

- [x] **Task G4.11** — Configure App Service settings
  - Run (substitute values from G4.10 and pick admin credentials for your two users):
    ```bash
    az webapp config connection-string set --name lifeworks-$SUFFIX --resource-group lifeworks-rg --settings DefaultConnection="<SQL_CONNECTION_STRING>" --connection-string-type SQLAzure

    az webapp config appsettings set --name lifeworks-$SUFFIX --resource-group lifeworks-rg --settings \
        Storage__BlobConnectionString="<STORAGE_CONNECTION_STRING>" \
        Storage__AttachmentsContainer="attachments" \
        Identity__SeedUsers__0__Email="you@example.com" \
        Identity__SeedUsers__0__Password="<STRONG_PASSWORD_1>" \
        Identity__SeedUsers__1__Email="wife@example.com" \
        Identity__SeedUsers__1__Password="<STRONG_PASSWORD_2>" \
        ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
    ```
  - The `__` (double underscore) translates to `:` in configuration keys

**Phase G4 commit:** none (no code changes). Save the suffix, passwords, and resource names in a private note.

---

# Phase G5 — Deployment via GitHub Actions

Goal: Push to `main` deploys automatically. EF migrations run on app startup (already wired).

- [x] **Task G5.1** — Generate publish profile credentials
  - In Azure Portal: Web App → Overview → "Get publish profile" → downloads `.PublishSettings`
  - Or run:
    ```bash
    az webapp deployment list-publishing-profiles --name lifeworks-$SUFFIX --resource-group lifeworks-rg --xml > publish-profile.xml
    ```

- [x] **Task G5.2** — Add publish profile as GitHub secret
  - In repo on GitHub: Settings → Secrets and variables → Actions → New repository secret
  - Name: `AZURE_WEBAPP_PUBLISH_PROFILE`
  - Value: the entire XML content
  - Delete the local `publish-profile.xml` after uploading

- [ ] **Task G5.3** — Add GitHub Actions workflow
  - **File:** `.github/workflows/deploy.yml` (new)
  - Content:
    ```yaml
    name: Deploy to Azure

    on:
      push:
        branches: [main]
      workflow_dispatch:

    jobs:
      build-and-deploy:
        runs-on: ubuntu-latest
        steps:
          - uses: actions/checkout@v4
          - uses: actions/setup-dotnet@v4
            with:
              dotnet-version: '10.0.x'
              dotnet-quality: 'preview'  # remove once .NET 10 GA
          - name: Restore
            run: dotnet restore
          - name: Build
            run: dotnet build --configuration Release --no-restore
          - name: Test
            run: dotnet test --configuration Release --no-build --verbosity normal
          - name: Publish
            run: dotnet publish src/LifeWorks.Web/LifeWorks.Web.csproj --configuration Release --output ./publish --no-build
          - name: Deploy
            uses: azure/webapps-deploy@v3
            with:
              app-name: lifeworks-${{ vars.AZURE_SUFFIX }}
              publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
              package: ./publish
    ```
  - Add a GitHub repository variable `AZURE_SUFFIX` matching the suffix from G4.1 (avoids hardcoding the URL)

- [ ] **Task G5.4** — Push to `main` and watch the workflow
  - Merge the feature branch to main
  - Watch: GitHub repo → Actions tab → "Deploy to Azure" run
  - On first deploy expect a 3–6 min build + 1–2 min deploy
  - If deploy fails, common causes: .NET 10 SDK availability on the runner (may need `dotnet-quality: 'preview'`), or missing publish profile secret

- [ ] **Task G5.5** — Verify first startup
  - Browse to `https://lifeworks-$SUFFIX.azurewebsites.net`
  - First request will be slow (15–60s): App Service cold start + SQL Serverless resume + first-run migrations
  - Expect to land on `/login`
  - Tail logs if anything looks off:
    ```bash
    az webapp log tail --name lifeworks-$SUFFIX --resource-group lifeworks-rg
    ```

**Phase G5 commit:** `ci: add GitHub Actions workflow for Azure deployment`

---

# Phase G6 — Security Hardening

Goal: Lock down the deployment beyond defaults.

- [ ] **Task G6.1** — Enable Forwarded Headers middleware
  - **File:** `src/LifeWorks.Web/Program.cs`
  - Before `app.UseHttpsRedirection()`:
    ```csharp
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });
    ```
  - Required because App Service terminates TLS at the front door; without this, `HttpContext.Request.IsHttps` is wrong and redirect loops can happen
  - The env var `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` set in G4.11 covers the same case automatically — pick one approach, not both

- [ ] **Task G6.2** — Verify HTTPS-only is enforced
  - Browse `http://lifeworks-$SUFFIX.azurewebsites.net` (note `http`)
  - Expect: automatic redirect to `https://`
  - Verify HSTS header present (browser dev tools → Network → response headers)

- [ ] **Task G6.3** — Verify Data Protection keys persist across restart
  - Log in, note your session cookie
  - Run: `az webapp restart --name lifeworks-$SUFFIX --resource-group lifeworks-rg`
  - Refresh the page — you should STILL be logged in (cookies survive the restart because keys are in Blob)
  - If you get logged out: G3.4 didn't take effect; check `Storage__BlobConnectionString` is set in app settings

- [ ] **Task G6.4** — Verify registration endpoint is 404
  - Browse `https://lifeworks-$SUFFIX.azurewebsites.net/register`
  - Expect: 404 (no registration page was added)

- [ ] **Task G6.5** — Optional: enable 2FA for both accounts
  - In Identity, MFA support is built in. If desired in a follow-up:
    - Add a `/account/manage/2fa` Blazor page calling `UserManager.GetAuthenticatorKeyAsync` / `SetTwoFactorEnabledAsync`
    - Display QR code (use `Qrcoder` NuGet or generate URL for `otpauth://`)
  - Defer to a follow-up plan if not doing immediately

**Phase G6 commit:** `feat(web): harden forwarded headers and verify security posture`

---

# Phase G7 — Verification & Cost Monitoring

- [ ] **Task G7.1** — Full end-to-end smoke from a phone
  - Open `https://lifeworks-$SUFFIX.azurewebsites.net` on mobile browser
  - Log in
  - Create a Property
  - Create a Home Improvement with an attachment (image from phone camera)
  - Log out and back in
  - Verify everything persists

- [ ] **Task G7.2** — Cost monitoring
  - In Azure Portal: Cost Management → Budgets → "Add budget"
  - Set monthly budget at $20 with alert at 75% ($15) emailed to your address
  - This catches surprises (e.g., SQL stuck "awake")

- [ ] **Task G7.3** — Confirm SQL auto-pause is working
  - After leaving the app idle 60+ minutes, run:
    ```bash
    az sql db show --resource-group lifeworks-rg --server lifeworks-sql-$SUFFIX --name LifeWorks --query status
    ```
  - Expected: `"Paused"`
  - On next browser hit, expect a 30–60s resume (one-time per idle period)

- [ ] **Task G7.4** — Document quirks in CLAUDE.md
  - **File:** `CLAUDE.md`
  - Add a brief "Hosting" section noting:
    - Production URL
    - Expected first-hit-of-day latency (cold start)
    - Resource group name and where suffix/passwords are stored (e.g., "1Password vault X")

**Phase G7 commit:** `docs: add Azure hosting notes to CLAUDE.md`

---

# Verification (overall)

- [ ] `dotnet build` — zero errors / zero warnings
- [ ] `dotnet test` — all tests pass
- [ ] GitHub Actions workflow run — green
- [ ] Production URL accessible over HTTPS, redirects from HTTP
- [ ] Login works; logout works; `/register` returns 404
- [ ] Attachment upload works (file lands in Blob, not local disk)
- [ ] App service restart does not invalidate sessions (Data Protection keys persist)
- [ ] Azure Cost Management budget configured with alert
- [ ] First-month actual cost recorded ≤ $15

---

# Rollback / Teardown

If you want to tear everything down: `az group delete --name lifeworks-rg --yes --no-wait` removes all resources in one command. No leftovers, no charges.

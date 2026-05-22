using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LifeWorks.Application;
using LifeWorks.Application.Services;
using LifeWorks.Infrastructure;
using LifeWorks.Infrastructure.Data;
using LifeWorks.Web;
using LifeWorks.Web.Components;
using LifeWorks.Web.Components.Account;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// No registration endpoint — accounts are seeded only
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

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationServices();

// Configure HSTS for production
builder.Services.AddHsts(o =>
{
    o.MaxAge = TimeSpan.FromDays(365);
    o.IncludeSubDomains = true;
});

// Configure Data Protection to persist keys to Azure Blob Storage when available
var blobConnForDp = builder.Configuration["Storage:BlobConnectionString"];
if (!string.IsNullOrEmpty(blobConnForDp))
{
    var dpBlobServiceClient = new BlobServiceClient(blobConnForDp);
    var dpContainerClient = dpBlobServiceClient.GetBlobContainerClient("data-protection");
    await dpContainerClient.CreateIfNotExistsAsync(PublicAccessType.None);
    var dpBlobClient = dpContainerClient.GetBlobClient("keys.xml");
    builder.Services.AddDataProtection()
        .PersistKeysToAzureBlobStorage(dpBlobClient)
        .SetApplicationName("LifeWorks");
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    await IdentitySeeder.SeedAsync(userManager, config);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
// HTTPS redirect is handled by Azure App Service (https-only: true) at the infrastructure level.
// Keeping UseHttpsRedirection here causes an infinite redirect loop because App Service forwards
// requests to the app as HTTP internally even after TLS termination.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/attachments/{id:guid}/download", async (Guid id, IAttachmentService attachmentService) =>
{
    try
    {
        var (fileStream, metadata) = await attachmentService.GetFileAsync(id);
        return Results.File(fileStream, metadata.ContentType, metadata.FileName);
    }
    catch (FileNotFoundException)
    {
        return Results.NotFound();
    }
});

// Minimal API login — bypasses Blazor streaming SSR so Set-Cookie works in a real browser POST
app.MapPost("/account/login", async (
    SignInManager<IdentityUser> signInManager,
    [FromForm] string? email,
    [FromForm] string? password,
    [FromForm] string? returnUrl,
    HttpContext httpContext) =>
{
    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        return Results.Redirect("/login?error=invalid");

    var result = await signInManager.PasswordSignInAsync(email, password, false, lockoutOnFailure: true);
    if (result.Succeeded)
    {
        var dest = Uri.IsWellFormedUriString(returnUrl, UriKind.Relative) ? returnUrl : "/";
        return Results.Redirect(dest);
    }
    return Results.Redirect("/login?error=invalid");
}).AllowAnonymous().DisableAntiforgery();

app.MapPost("/logout", async (SignInManager<IdentityUser> signInManager, [FromForm] string? returnUrl) =>
{
    await signInManager.SignOutAsync();
    return Results.LocalRedirect(returnUrl ?? "/login");
}).RequireAuthorization();

app.Run();

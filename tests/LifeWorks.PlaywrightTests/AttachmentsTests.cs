using System.Text.RegularExpressions;
using Microsoft.Playwright.NUnit;
using Microsoft.Playwright;

namespace LifeWorks.PlaywrightTests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class AttachmentsTests : PageTest
{
    private const string BaseUrl = "http://localhost:5053";
    private const string TestFileContent = "%PDF-1.4\n1 0 obj<</Type/Catalog>>endobj\ntrailer<</Root 1 0 R>>";

    public override BrowserNewContextOptions ContextOptions() => new() { BaseURL = BaseUrl };

    private async Task LoginAsync()
    {
        var resp = await Page.APIRequest.PostAsync($"{BaseUrl}/account/login", new()
        {
            DataString = "email=dev%40lifeworks.local&password=DevPassword123%21",
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/x-www-form-urlencoded"
            }
        });
        Console.WriteLine($"Login API response: {resp.Status}");
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        if (Page.Url.Contains("/login"))
            throw new InvalidOperationException($"Login failed — still at {Page.Url}");
    }

    private async Task<string> CreateImprovementAndOpenDetail(string title)
    {
        await Page.GotoAsync("/improvements/new");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForFunctionAsync("() => typeof window.Blazor !== 'undefined'", null, new() { Timeout = 15000 });
        await Page.WaitForFunctionAsync("() => !document.querySelector('blazor-ssr-end')", null, new() { Timeout = 5000 });
        await Page.WaitForTimeoutAsync(500);

        await Page.Locator(".mud-select").Nth(0).ClickAsync();
        await Page.Locator(".mud-popover-open .mud-list-item-clickable").First.WaitForAsync(new() { Timeout = 10000 });
        await Page.Locator(".mud-popover-open .mud-list-item-clickable").First.ClickAsync();
        await Page.WaitForTimeoutAsync(300);

        await Page.Locator(".mud-select").Nth(2).ClickAsync();
        await Page.Locator(".mud-popover-open .mud-list-item-clickable").First.WaitForAsync(new() { Timeout = 10000 });
        await Page.Locator(".mud-popover-open .mud-list-item-clickable").First.ClickAsync();
        await Page.WaitForTimeoutAsync(300);

        var titleInput = Page.Locator("input[type='text'][inputmode='text']:not([readonly])").First;
        await titleInput.ClickAsync();
        await titleInput.FillAsync(title);

        var dateInput = Page.Locator(".mud-picker input:not([readonly])").First;
        await dateInput.ClickAsync();
        await dateInput.FillAsync("01/15/2024");
        await Page.Keyboard.PressAsync("Escape");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
        await Page.WaitForURLAsync("**/improvements", new() { Timeout = 15000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var row = Page.Locator("tr.mud-table-row, .mud-data-grid-row").Filter(new() { HasText = title });
        await row.Locator("button").First.ClickAsync();
        await Page.WaitForURLAsync(new Regex(@"/improvements/[0-9a-f\-]+$"));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        return title;
    }

    private async Task<string> UploadTestFileAsync()
    {
        var fileName = $"test-receipt-{Guid.NewGuid():N}.pdf";
        var filePath = Path.Combine(Path.GetTempPath(), fileName);
        await File.WriteAllTextAsync(filePath, TestFileContent);

        await Page.SetInputFilesAsync("input[type='file']", filePath);
        await Page.WaitForTimeoutAsync(1500);
        await Expect(Page.Locator(".mud-list-item").Filter(new() { HasText = fileName })).ToBeVisibleAsync(new() { Timeout = 10000 });

        return fileName;
    }

    [Test]
    [Order(1)]
    public async Task UploadAttachment_AppearsWithNameAndSize()
    {
        await LoginAsync();
        var title = $"Attachment Test {Guid.NewGuid():N}";
        await CreateImprovementAndOpenDetail(title);

        await Expect(Page.Locator("text=No attachments yet.")).ToBeVisibleAsync();

        var fileName = await UploadTestFileAsync();

        var item = Page.Locator(".mud-list-item").Filter(new() { HasText = fileName });
        await Expect(item).ToContainTextAsync(new Regex(@"\d+(\.\d+)?\s*B - "));
    }

    [Test]
    [Order(2)]
    public async Task DownloadAttachment_ReturnsCorrectContentType()
    {
        await LoginAsync();
        var title = $"Download Test {Guid.NewGuid():N}";
        await CreateImprovementAndOpenDetail(title);
        var fileName = await UploadTestFileAsync();

        var item = Page.Locator(".mud-list-item").Filter(new() { HasText = fileName });
        var downloadHref = await item.Locator("a[href*='/attachments/'][href*='/download']").GetAttributeAsync("href");
        Assert.That(downloadHref, Is.Not.Null.And.Not.Empty);

        var resp = await Page.APIRequest.GetAsync($"{BaseUrl}{downloadHref}");
        Assert.That(resp.Status, Is.EqualTo(200));
        Assert.That(resp.Headers["content-type"], Is.EqualTo("application/pdf"));
        var disposition = resp.Headers.TryGetValue("content-disposition", out var cd) ? cd : "";
        Assert.That(disposition, Does.Contain(fileName));
    }

    [Test]
    [Order(3)]
    public async Task DeleteAttachment_RemovesFromListAndShowsEmptyState()
    {
        await LoginAsync();
        var title = $"Delete Attachment Test {Guid.NewGuid():N}";
        await CreateImprovementAndOpenDetail(title);
        var fileName = await UploadTestFileAsync();

        var item = Page.Locator(".mud-list-item").Filter(new() { HasText = fileName });
        await item.Locator("button").ClickAsync();

        var dialog = Page.Locator(".mud-dialog");
        await dialog.WaitForAsync(new() { Timeout = 5000 });
        await Page.Locator(".mud-message-box__yes-button").ClickAsync();
        await Page.WaitForTimeoutAsync(1500);

        await Expect(Page.Locator(".mud-list-item").Filter(new() { HasText = fileName })).Not.ToBeVisibleAsync();
        await Expect(Page.Locator("text=No attachments yet.")).ToBeVisibleAsync();
    }

    [Test]
    [Order(4)]
    public async Task ImprovementsList_ShowsAttachmentCountBadge()
    {
        await LoginAsync();
        var title = $"Badge Test {Guid.NewGuid():N}";
        await CreateImprovementAndOpenDetail(title);
        await UploadTestFileAsync();

        await Page.GotoAsync("/improvements");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var row = Page.Locator("tr.mud-table-row, .mud-data-grid-row").Filter(new() { HasText = title });
        await Expect(row).ToContainTextAsync("1");
    }
}

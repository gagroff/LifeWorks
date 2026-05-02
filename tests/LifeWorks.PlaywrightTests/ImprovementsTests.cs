using System.Text.RegularExpressions;
using Microsoft.Playwright.NUnit;
using Microsoft.Playwright;

namespace LifeWorks.PlaywrightTests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class ImprovementsTests : PageTest
{
    private const string BaseUrl = "http://localhost:5053";

    public override BrowserNewContextOptions ContextOptions() => new() { BaseURL = BaseUrl };

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<string> CreateImprovement(string title, string? cost = null)
    {
        await Page.GotoAsync("/improvements/new");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait for Blazor circuit to connect and initial render to complete
        await Page.WaitForFunctionAsync("() => typeof window.Blazor !== 'undefined'", null, new() { Timeout = 15000 });
        await Page.WaitForFunctionAsync("() => !document.querySelector('blazor-ssr-end')", null, new() { Timeout = 5000 });
        await Page.WaitForTimeoutAsync(500);

        // Each MudSelect renders as 2 nested .mud-select divs: [0,1]=Property, [2,3]=Category, [4,5]=Contractor
        // Property — Nth(0) is the outer Property select
        await Page.Locator(".mud-select").Nth(0).ClickAsync();
        await Page.Locator(".mud-popover-open .mud-list-item-clickable").First.WaitForAsync(new() { Timeout = 10000 });
        await Page.Locator(".mud-popover-open .mud-list-item-clickable").First.ClickAsync();
        await Page.WaitForTimeoutAsync(300);

        // Category — Nth(2) is the outer Category select
        await Page.Locator(".mud-select").Nth(2).ClickAsync();
        await Page.Locator(".mud-popover-open .mud-list-item-clickable").First.WaitForAsync(new() { Timeout = 10000 });
        await Page.Locator(".mud-popover-open .mud-list-item-clickable").First.ClickAsync();
        await Page.WaitForTimeoutAsync(300);

        // Title — fill after dropdowns to avoid triggering re-renders that reset them
        var titleInput = Page.Locator("input[type='text'][inputmode='text']:not([readonly])").First;
        await titleInput.ClickAsync();
        await titleInput.FillAsync(title);

        // Date Completed — first editable MudDatePicker input
        var dateInput = Page.Locator(".mud-picker input:not([readonly])").First;
        await dateInput.ClickAsync();
        await dateInput.FillAsync("01/15/2024");
        await Page.Keyboard.PressAsync("Escape");
        Console.WriteLine($"Date filled: '{await dateInput.InputValueAsync()}'");

        if (cost is not null)
        {
            var costInput = Page.Locator("input[inputmode='decimal'], input[type='number']").First;
            await costInput.ClickAsync();
            await costInput.FillAsync(cost);
            await costInput.PressAsync("Tab");
        }

        await Page.Locator("button[type='submit']").ClickAsync();
        await Page.WaitForURLAsync("**/improvements", new() { Timeout = 15000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        return title;
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task ImprovementsListPage_Loads()
    {
        await Page.GotoAsync("/improvements");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page).ToHaveTitleAsync(new Regex("Home Improvements", RegexOptions.IgnoreCase));
        await Expect(Page.Locator("h4").First).ToContainTextAsync("Home Improvements");
    }

    [Test]
    public async Task NavMenu_ImprovementsLinkWorks()
    {
        await Page.GotoAsync("/improvements");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.Locator("h4").First).ToContainTextAsync("Home Improvements");

        // Nav link exists and points to the right route
        var navLink = Page.Locator("a[href='improvements'], a[href='/improvements']");
        await Expect(navLink).ToBeVisibleAsync();
    }

    [Test]
    public async Task CreateImprovement_WithCost_AppearsInList()
    {
        var title = $"Roof Repair {Guid.NewGuid():N}";
        await CreateImprovement(title, cost: "1500");

        await Expect(Page.Locator($"text={title}")).ToBeVisibleAsync();
        await Expect(Page.Locator(".mud-data-grid").First).ToContainTextAsync("1,500");
    }

    [Test]
    public async Task CreateDiyImprovement_NoCost_ShowsDiyLabel()
    {
        var title = $"DIY Paint {Guid.NewGuid():N}";
        await CreateImprovement(title);

        // Row containing this title should show "DIY"
        var row = Page.Locator("tr.mud-table-row, .mud-data-grid-row").Filter(new() { HasText = title });
        await Expect(row).ToBeVisibleAsync();
        await Expect(row).ToContainTextAsync("DIY");
    }

    [Test]
    public async Task RunningTotal_IsDisplayedOnListPage()
    {
        await Page.GotoAsync("/improvements");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.Locator("text=/Total Cost/i")).ToBeVisibleAsync();
    }

    [Test]
    public async Task FilterApplyAndClear_DoesNotError()
    {
        await Page.GotoAsync("/improvements");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Apply filter (without selecting anything — just click Apply)
        await Page.Locator("button", new() { HasText = "Apply" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.Locator("h4").First).ToContainTextAsync("Home Improvements");

        // Clear filter
        await Page.Locator("button", new() { HasText = "Clear" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.Locator("h4").First).ToContainTextAsync("Home Improvements");
    }

    [Test]
    public async Task EditImprovement_FormPrePopulates_AndSaves()
    {
        var original = $"Edit Me {Guid.NewGuid():N}";
        var updated = $"Edited {Guid.NewGuid():N}";
        await CreateImprovement(original);

        await Page.GotoAsync("/improvements");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Click the Edit button (2nd action button) on the matching row
        var row = Page.Locator("tr.mud-table-row, .mud-data-grid-row").Filter(new() { HasText = original });
        var editBtn = row.Locator("button").Nth(1);
        await editBtn.ClickAsync();

        await Page.WaitForURLAsync(new Regex(@"/improvements/.*edit"));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForFunctionAsync("() => typeof window.Blazor !== 'undefined'", null, new() { Timeout = 15000 });
        await Page.WaitForFunctionAsync("() => !document.querySelector('blazor-ssr-end')", null, new() { Timeout = 5000 });
        await Page.WaitForTimeoutAsync(500);

        // Title field should have the original value
        var titleField = Page.Locator("input[type='text'][inputmode='text']:not([readonly])").First;
        var currentValue = await titleField.InputValueAsync();
        Assert.That(currentValue, Is.EqualTo(original));

        // Update title
        await titleField.ClickAsync();
        await titleField.FillAsync(updated);
        await Page.Locator("button[type='submit']").ClickAsync();
        await Page.WaitForURLAsync("**/improvements");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.Locator($"text={updated}")).ToBeVisibleAsync();
    }

    [Test]
    public async Task DetailView_ShowsImprovementInfo()
    {
        var title = $"Detail Test {Guid.NewGuid():N}";
        await CreateImprovement(title, cost: "250");

        await Page.GotoAsync("/improvements");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Click the View button (1st action button) on the matching row
        var row = Page.Locator("tr.mud-table-row, .mud-data-grid-row").Filter(new() { HasText = title });
        await row.Locator("button").First.ClickAsync();

        await Page.WaitForURLAsync(new Regex(@"/improvements/[0-9a-f\-]+$"));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.Locator($"text={title}")).ToBeVisibleAsync();
        // No contractor selected — should show DIY section
        await Expect(Page.Locator("text=DIY")).ToBeVisibleAsync();
    }

    [Test]
    public async Task DeleteImprovement_RemovesFromList()
    {
        var title = $"To Delete {Guid.NewGuid():N}";
        await CreateImprovement(title);

        await Page.GotoAsync("/improvements");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var row = Page.Locator("tr.mud-table-row, .mud-data-grid-row").Filter(new() { HasText = title });
        // Delete button is 3rd action button (view=0, edit=1, delete=2)
        await row.Locator("button").Nth(2).ClickAsync();

        // Confirm in MudMessageBox — wait for dialog then click the Yes/Delete button
        var dialog = Page.Locator(".mud-dialog");
        await dialog.WaitForAsync(new() { Timeout = 5000 });
        // Use the MudMessageBox yes button class for precise targeting
        await Page.Locator(".mud-message-box__yes-button").ClickAsync();
        await Page.WaitForTimeoutAsync(3000);
        // Force a full page reload (GotoAsync on same URL may not reinitialize Blazor component)
        await Page.ReloadAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.Locator("td.mud-table-cell").Filter(new() { HasText = title })).Not.ToBeVisibleAsync();
    }
}

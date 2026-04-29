using System.Text.RegularExpressions;
using Microsoft.Playwright.NUnit;
using Microsoft.Playwright;

namespace LifeWorks.PlaywrightTests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class DomInspector : PageTest
{
    private const string BaseUrl = "http://localhost:5053";

    public override BrowserNewContextOptions ContextOptions() => new() { BaseURL = BaseUrl };

    [Test]
    public async Task TryFillTitleInput()
    {
        await Page.GotoAsync("/improvements/new");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var titleInput = Page.Locator("input[type='text'][inputmode='text']:not([readonly])").First;
        await titleInput.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });

        var editable = await titleInput.IsEditableAsync();
        Console.WriteLine($"editable={editable}");

        await titleInput.ClickAsync();
        Console.WriteLine("Click OK");
        await titleInput.FillAsync("Hello");
        Console.WriteLine("FillAsync OK");
        var val = await titleInput.InputValueAsync();
        Console.WriteLine($"Value: '{val}'");
        Assert.That(val, Is.EqualTo("Hello"));
    }

    [Test]
    public async Task TryFullCreateFlow()
    {
        await Page.GotoAsync("/improvements/new");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var titleInput = Page.Locator("input[type='text'][inputmode='text']:not([readonly])").First;
        await titleInput.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
        await titleInput.ClickAsync();
        await titleInput.FillAsync("Test Title 123");

        // Wait for Blazor circuit to connect and re-render
        await Page.WaitForFunctionAsync("() => typeof window.Blazor !== 'undefined'", null, new() { Timeout = 10000 });
        Console.WriteLine("Blazor circuit connected");

        // Wait for the circuit to finish the initial render cycle
        // A reliable signal: the select input no longer shows Guid.Empty text OR just wait
        await Page.WaitForFunctionAsync(
            "() => !document.querySelector('blazor-ssr-end')",
            null, new() { Timeout = 10000 });
        Console.WriteLine("Post-circuit render complete");
        await Page.WaitForTimeoutAsync(500);

        // Open property dropdown
        await Page.Locator(".mud-select").First.ClickAsync();
        await Page.Locator(".mud-list-item-clickable").First.WaitForAsync(new() { Timeout = 10000 });
        var firstItemText = await Page.Locator(".mud-list-item-clickable").First.InnerTextAsync();
        Console.WriteLine($"List item text: '{firstItemText}'");

        // Click and check for DOM changes (a websocket message should go back to server)
        var wsMessages = new List<string>();
        Page.WebSocket += (_, ws) => {
            ws.FrameReceived += (_, f) => wsMessages.Add($"recv:{f.Text?[..Math.Min(50, f.Text?.Length ?? 0)]}");
            ws.FrameSent += (_, f) => wsMessages.Add($"sent:{f.Text?[..Math.Min(50, f.Text?.Length ?? 0)]}");
        };

        await Page.Locator(".mud-list-item-clickable").First.ClickAsync();
        await Page.WaitForTimeoutAsync(1000);
        Console.WriteLine($"WS messages after click: {wsMessages.Count}");
        await Page.Keyboard.PressAsync("Escape");

        var selectInputValue = await Page.Locator(".mud-select").First.Locator("input[readonly]").InputValueAsync();
        Console.WriteLine($"Select input value after click: '{selectInputValue}'");

        Assert.Pass("Property selection checked");
    }

    [Test]
    public async Task InspectFormInputs()
    {
        await Page.GotoAsync("/improvements/new");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.Locator("button[type='submit']").WaitForAsync(new() { Timeout = 15000 });

        var inputs = Page.Locator("input");
        var count = await inputs.CountAsync();
        Console.WriteLine($"Total inputs: {count}");
        for (int i = 0; i < Math.Min(count, 15); i++)
        {
            var inp = inputs.Nth(i);
            var type = await inp.GetAttributeAsync("type") ?? "none";
            var cls = await inp.GetAttributeAsync("class") ?? "";
            var ro = await inp.GetAttributeAsync("readonly");
            var disabled = await inp.GetAttributeAsync("disabled");
            var inputmode = await inp.GetAttributeAsync("inputmode") ?? "";
            Console.WriteLine($"  [{i}] type={type} readonly={ro != null} disabled={disabled != null} inputmode={inputmode} class={cls[..Math.Min(60, cls.Length)]}");
        }
        Assert.Pass();
    }

    [Test]
    public async Task InspectListPageHtml()
    {
        await Page.GotoAsync("/improvements");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(1000);

        Console.WriteLine("h4 count: " + await Page.Locator("h4").CountAsync());
        Console.WriteLine("h4 text: " + await Page.Locator("h4").First.InnerTextAsync());
        Console.WriteLine("nav a improvements: " + await Page.Locator("a[href='improvements']").CountAsync());
        Console.WriteLine("Total Cost text: " + await Page.Locator("text=Total Cost").CountAsync());
        Console.WriteLine(".mud-typography: " + await Page.Locator(".mud-typography:has-text('Total')").CountAsync());
        Assert.Pass();
    }

    [Test]
    public async Task DiagnoseCategorySelect()
    {
        await Page.GotoAsync("/improvements/new");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForFunctionAsync("() => typeof window.Blazor !== 'undefined'", null, new() { Timeout = 15000 });
        await Page.WaitForFunctionAsync("() => !document.querySelector('blazor-ssr-end')", null, new() { Timeout = 5000 });
        await Page.WaitForTimeoutAsync(1000);

        Console.WriteLine($"Total .mud-select count: {await Page.Locator(".mud-select").CountAsync()}");
        for (int i = 0; i < await Page.Locator(".mud-select").CountAsync(); i++)
        {
            var label = await Page.Locator(".mud-select").Nth(i).Locator("label").TextContentAsync().ConfigureAwait(false);
            Console.WriteLine($"  mud-select[{i}] label: '{label}'");
        }

        // Click Property select
        Console.WriteLine("=== PROPERTY ===");
        await Page.Locator(".mud-select").First.ClickAsync();
        await Page.WaitForTimeoutAsync(500);
        Console.WriteLine($"mud-popover-open count: {await Page.Locator(".mud-popover-open").CountAsync()}");
        Console.WriteLine($"visible list items: {await Page.Locator(".mud-popover-open .mud-list-item-clickable").CountAsync()}");
        await Page.Locator(".mud-popover-open .mud-list-item-clickable").First.ClickAsync();
        await Page.WaitForTimeoutAsync(500);
        Console.WriteLine($"After property click - popover-open count: {await Page.Locator(".mud-popover-open").CountAsync()}");
        var propVal = await Page.Locator(".mud-select").First.Locator("input[readonly]").InputValueAsync();
        Console.WriteLine($"Property selected value: '{propVal}'");

        // Click Category select
        Console.WriteLine("=== CATEGORY ===");
        await Page.Locator(".mud-select").Nth(1).ClickAsync();
        await Page.WaitForTimeoutAsync(500);
        Console.WriteLine($"mud-popover-open count: {await Page.Locator(".mud-popover-open").CountAsync()}");
        Console.WriteLine($"visible list items: {await Page.Locator(".mud-popover-open .mud-list-item-clickable").CountAsync()}");
        var items = await Page.Locator(".mud-popover-open .mud-list-item-clickable").AllTextContentsAsync();
        Console.WriteLine($"Items: {string.Join(", ", items.Take(5))}");
        if (items.Count > 0)
        {
            await Page.Locator(".mud-popover-open .mud-list-item-clickable").First.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
        }
        var catVal = await Page.Locator(".mud-select").Nth(1).Locator("input[readonly]").InputValueAsync();
        Console.WriteLine($"Category selected value: '{catVal}'");

        Assert.Pass("Diagnostics complete");
    }

    [Test]
    public async Task InspectSelectDropdownHtml()
    {
        await Page.GotoAsync("/improvements/new");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Click the first MudSelect to open dropdown
        await Page.Locator(".mud-select").First.ClickAsync();
        await Page.WaitForTimeoutAsync(1000);

        var html = await Page.ContentAsync();
        await File.WriteAllTextAsync(@"C:\code\LifeWorks\tests\select-open-dom.html", html);

        // Try various list item selectors
        Console.WriteLine(".mud-list-item: " + await Page.Locator(".mud-list-item").CountAsync());
        Console.WriteLine(".mud-list-item-clickable: " + await Page.Locator(".mud-list-item-clickable").CountAsync());
        Console.WriteLine(".mud-popover .mud-list-item: " + await Page.Locator(".mud-popover .mud-list-item").CountAsync());
        Console.WriteLine("[role=option]: " + await Page.Locator("[role='option']").CountAsync());
        Console.WriteLine(".mud-popover: " + await Page.Locator(".mud-popover").CountAsync());
        Console.WriteLine(".mud-popover-open: " + await Page.Locator(".mud-popover-open").CountAsync());
        Console.WriteLine("li: " + await Page.Locator("li").CountAsync());
        Console.WriteLine(".mud-select-popover: " + await Page.Locator(".mud-select-popover").CountAsync());

        Assert.Pass("DOM captured after select click");
    }
}

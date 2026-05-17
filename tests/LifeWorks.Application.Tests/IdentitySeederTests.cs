using LifeWorks.Infrastructure.Data;
using LifeWorks.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LifeWorks.Application.Tests;

public class IdentitySeederTests
{
    private static async Task<(UserManager<IdentityUser>, IServiceScope)> BuildUserManagerAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole());
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentity<IdentityUser, IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();
        var sp = services.BuildServiceProvider();
        var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        return (scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>(), scope);
    }

    private static IConfiguration BuildConfig(params (string Email, string Password)[] users)
    {
        var dict = new Dictionary<string, string?>();
        for (int i = 0; i < users.Length; i++)
        {
            dict[$"Identity:SeedUsers:{i}:Email"] = users[i].Email;
            dict[$"Identity:SeedUsers:{i}:Password"] = users[i].Password;
        }
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    [Fact]
    public async Task Seed_WithTwoUsers_CreatesBothInDatabase()
    {
        var (userManager, _) = await BuildUserManagerAsync();
        var config = BuildConfig(("a@test.com", "Password123!@#"), ("b@test.com", "Password456!@#"));

        await IdentitySeeder.SeedAsync(userManager, config);

        Assert.NotNull(await userManager.FindByEmailAsync("a@test.com"));
        Assert.NotNull(await userManager.FindByEmailAsync("b@test.com"));
    }

    [Fact]
    public async Task Seed_WhenUserAlreadyExists_DoesNotDuplicate()
    {
        var (userManager, _) = await BuildUserManagerAsync();
        var config = BuildConfig(("a@test.com", "Password123!@#"));

        await IdentitySeeder.SeedAsync(userManager, config);
        await IdentitySeeder.SeedAsync(userManager, config); // second call

        var users = await userManager.Users.ToListAsync();
        Assert.Single(users);
    }

    [Fact]
    public async Task Seed_WithEmptyConfig_DoesNothing()
    {
        var (userManager, _) = await BuildUserManagerAsync();
        var config = new ConfigurationBuilder().Build();

        await IdentitySeeder.SeedAsync(userManager, config); // should not throw

        Assert.Empty(await userManager.Users.ToListAsync());
    }
}

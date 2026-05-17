using Microsoft.AspNetCore.Identity;

namespace LifeWorks.Web;

public static class IdentitySeeder
{
    public static async Task SeedAsync(UserManager<IdentityUser> userManager, IConfiguration config)
    {
        var seedUsers = config.GetSection("Identity:SeedUsers").GetChildren().ToList();
        foreach (var userConfig in seedUsers)
        {
            var email = userConfig["Email"];
            var password = userConfig["Password"];
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) continue;

            var existing = await userManager.FindByEmailAsync(email);
            if (existing is not null) continue;

            var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Failed to seed user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }
}

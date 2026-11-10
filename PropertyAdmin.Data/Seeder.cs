using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PropertyAdmin.Data;

public static class Seeder
{
    public static readonly IReadOnlyDictionary<string, string[]> DefaultRoles = new Dictionary<string, string[]>
    {
        ["Admin"] = [.. Permissions.All.Where(p => p != Permissions.ReadOwn)],
        ["Manager"] = [Permissions.OwnersAssets, Permissions.ExpensesActivities, Permissions.Invoices, Permissions.Reports, Permissions.ReadAll],
        ["Staff"] = [Permissions.ExpensesActivities, Permissions.ReadAll],
        ["Finance"] = [Permissions.VatRates, Permissions.Invoices, Permissions.Reports, Permissions.ReadAll],
        ["Viewer"] = [Permissions.Reports, Permissions.ReadAll],
        ["Owner"] = [Permissions.ReadOwn],
    };

    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await roleManager.Roles.AnyAsync())
        {
            foreach (var (name, permissions) in DefaultRoles)
            {
                var role = new IdentityRole(name);
                await roleManager.CreateAsync(role);
                foreach (var p in permissions)
                    await roleManager.AddClaimAsync(role, new Claim(Permissions.ClaimType, p));
            }
        }

        if (!await db.VatRates.AnyAsync())
        {
            var from = new DateOnly(2019, 1, 1);
            db.VatRates.AddRange(
                new VatRate { Name = "Hoog", Percentage = 21m, ValidFrom = from },
                new VatRate { Name = "Laag", Percentage = 9m, ValidFrom = from },
                new VatRate { Name = "Nul", Percentage = 0m, ValidFrom = from });
            await db.SaveChangesAsync();
        }

        if (!await userManager.Users.AnyAsync())
        {
            var userName = config["Seed:AdminUserName"];
            var email = config["Seed:AdminEmail"];
            var password = config["Seed:AdminPassword"];
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("No users exist. Set Seed:AdminUserName, Seed:AdminEmail and Seed:AdminPassword to create the first admin.");

            var admin = new ApplicationUser { UserName = userName, Email = email, EmailConfirmed = true };
            var result = await userManager.CreateAsync(admin, password);
            if (!result.Succeeded)
                throw new InvalidOperationException("Seed admin could not be created: " + string.Join("; ", result.Errors.Select(e => e.Description)));
            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}

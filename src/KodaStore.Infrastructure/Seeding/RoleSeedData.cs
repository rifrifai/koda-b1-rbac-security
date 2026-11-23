using System;
using KodaStore.Application.Security;
using KodaStore.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace KodaStore.Infrastructure.Seeding;

public static class RoleSeedData
{
    public static async Task SeedAsync(
        RoleManager<IdentityRole<Guid>> roleManager,
        UserManager<AppUser> userManager,
        string? bootstrapAdminEmail
    )
    {
        string[] requiredRoles = [AppRoles.Customer, AppRoles.Admin];

        foreach (var roleName in requiredRoles)
        {
            if (await roleManager.RoleExistsAsync(roleName))
                continue;

            var result = await roleManager.CreateAsync(
                new IdentityRole<Guid>
                {
                    Name = roleName
                });
            EnsureSucceeded(result, $"Membuat role {roleName}");
        }

        if (string.IsNullOrWhiteSpace(bootstrapAdminEmail))
            return;

        var admin = await userManager.FindByEmailAsync(bootstrapAdminEmail.Trim()) 
                        ?? throw new InvalidOperationException("Akun bootstrap admin belum terdaftar.");

        if (!await userManager.IsInRoleAsync(
            admin,
            AppRoles.Admin))
        {
            var result = await userManager.AddToRoleAsync(
                admin,
                AppRoles.Admin);

            EnsureSucceeded(result, "Memberikan role ADMIN");
        }

        if (await userManager.IsInRoleAsync(admin, AppRoles.Customer))
        {
            var result = await userManager.RemoveFromRoleAsync(
                admin,
                AppRoles.Customer);

            EnsureSucceeded(result, "Menghapus role CUSTOMER dari administrator");
        }
    }

    private static void EnsureSucceeded(
        IdentityResult result, string operation)
    {
        if (result.Succeeded)
            return;

        var description = string.Join("; ", result.Errors.Select(error => error.Description));

        throw new InvalidOperationException($"{operation} gagal: {description}");
    }
}

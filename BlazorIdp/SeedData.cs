using System.Security.Claims;
using BlazorIdp.Data;
using BlazorIdp.Models;
using IdentityModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlazorIdp;

internal static class SeedData
{
    internal static void EnsureSeedData(this WebApplication app)
    {
        using var scope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureDeleted();
        context.Database.Migrate();

        using var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var alice = userMgr.FindByNameAsync("alice").Result;
        if (alice is null)
        {
            alice = new ApplicationUser
            {
                UserName = "alice@email.com",
                Email = "alice@email.com",
                EmailConfirmed = true,
                FavoriteColor = "red"
            };
            var result = userMgr.CreateAsync(alice, "Pass123$").Result;
            if (!result.Succeeded)
                throw new Exception(result.Errors.First().Description);
            result = userMgr.AddClaimsAsync(alice, [
                new Claim(JwtClaimTypes.Name, "Alice Smith"),
                new Claim(JwtClaimTypes.GivenName, "Alice"),
                new Claim(JwtClaimTypes.FamilyName, "Smith")
            ]).Result;
            if (!result.Succeeded)
                throw new Exception(result.Errors.First().Description);
        }
    }
}
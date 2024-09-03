using Microsoft.AspNetCore.Identity;

namespace BlazorIdp.Models;

internal sealed class ApplicationUser : IdentityUser
{
    public string? FavoriteColor { get; set; }
}
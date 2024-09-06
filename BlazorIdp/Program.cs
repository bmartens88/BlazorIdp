using BlazorIdp;
using BlazorIdp.Components;
using BlazorIdp.Components.Account;
using BlazorIdp.Data;
using BlazorIdp.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

using var connection = new SqliteConnection("Data Source = :memory:");
await connection.OpenAsync();

builder.Services.AddDbContext<ApplicationDbContext>(
    opts => opts.UseSqlite(connection));

builder.Services.AddIdentityCore<ApplicationUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddIdentityServer()
    .AddAspNetIdentity<ApplicationUser>()
    .AddInMemoryIdentityResources(Config.IdentityResources)
    .AddInMemoryApiScopes(Config.ApiScopes)
    .AddInMemoryClients(Config.Clients)
    .AddProfileService<CustomUserProfileService>();

builder.Services.AddAuthentication(opts =>
    {
        opts.DefaultScheme = IdentityConstants.ApplicationScheme;
        opts.DefaultChallengeScheme = IdentityConstants.ExternalScheme;
    })
    .AddGoogle(opts =>
    {
        opts.SignInScheme = IdentityConstants.ExternalScheme;
        opts.ClientSecret = "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";
        opts.ClientId = "1050117780897-61c1cb1v8dsu7ohdd5rpmm02te17mkik.apps.googleusercontent.com";
    })
    .AddIdentityCookies();

builder.Services.AddAuthorization();

// Add services to the container.
builder.Services.AddRazorComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseIdentityServer();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>();
app.MapAdditionalIdentityEndpoints();

app.EnsureSeedData();

app.Run();
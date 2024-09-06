using System.Security.Claims;
using BlazorIdp.Components.Account.Pages;
using BlazorIdp.Models;
using Duende.IdentityServer;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Services;
using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace BlazorIdp.Components.Account;

internal static class IdentityRouteBuilderExtensions
{
    internal static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/Account");

        group.MapPost("/PerformExternalLogin", (
            HttpContext context,
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromForm] string provider,
            [FromForm] string returnUrl) =>
        {
            IEnumerable<KeyValuePair<string, StringValues>> query =
            [
                new("ReturnUrl", returnUrl),
                new("Action", ExternalLogin.LoginCallbackAction)
            ];

            var redirectUrl = UriHelper.BuildRelative(
                context.Request.PathBase,
                "/Account/ExternalLogin",
                QueryString.Create(query));

            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return TypedResults.Challenge(properties, [provider]);
        });

        group.MapPost("/Logout", async (
            HttpContext httpContext,
            ClaimsPrincipal user,
            IIdentityServerInteractionService interaction,
            string? returnUrl,
            string? logoutId) =>
        {
            var showLogoutPrompt = true;
            if (user.Identity?.IsAuthenticated is not true)
                showLogoutPrompt = false;
            else
            {
                var context = await interaction.GetLogoutContextAsync(logoutId);
                if (context.ShowSignoutPrompt is false)
                    showLogoutPrompt = false;
            }

            if (!showLogoutPrompt)
                return TypedResults.LocalRedirect("/Account/testerke");

            IEnumerable<KeyValuePair<string, StringValues>> query =
            [
                new("logoutId", logoutId),
                new("returnUrl", returnUrl)
            ];
            var url = UriHelper.BuildRelative(
                httpContext.Request.PathBase,
                "/Account/ConfirmLogout",
                QueryString.Create(query));
            return TypedResults.LocalRedirect(url);
        });

        group.MapPost("/PerformLogout", async (
            ClaimsPrincipal user,
            HttpContext httpContext,
            [FromServices] IIdentityServerInteractionService interaction,
            [FromServices] IEventService events,
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromServices] IdentityRedirectManager redirectManager,
            [FromForm] string? logoutId,
            [FromForm] string? returnUrl,
            [FromForm] bool choice) =>
        {
            if (!choice || user.Identity?.IsAuthenticated is not true)
                return TypedResults.LocalRedirect($"~/{returnUrl}");

            logoutId ??= await interaction.CreateLogoutContextAsync();

            await signInManager.SignOutAsync();

            await events.RaiseAsync(new UserLogoutSuccessEvent(user.GetSubjectId(), user.GetDisplayName()));

            var idp = user.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;

            if (idp is not null and not IdentityServerConstants.LocalIdentityProvider)
            {
                if (await GetSchemeSupportsSignOutAsync(httpContext, idp))
                {
                    var url = $"/Account/LoggedOut?logoutId={logoutId}";
                    await httpContext.SignOutAsync(idp, new AuthenticationProperties
                    {
                        RedirectUri = url
                    });
                }
            }

            IEnumerable<KeyValuePair<string, StringValues>> query = [new("logoutId", logoutId)];
            var redirectUrl = GetRedirectUrl(httpContext, returnUrl, query);

            return TypedResults.LocalRedirect(redirectUrl);
        });

        return group;
    }

    private static string GetRedirectUrl(HttpContext httpContext, string? returnUrl,
        IEnumerable<KeyValuePair<string, StringValues>>? query)
    {
        var url = UriHelper.BuildRelative(
            httpContext.Request.PathBase,
            returnUrl ?? "~/",
            query is not null ? QueryString.Create(query) : QueryString.Empty);
        return url;
    }

    private static async Task<bool> GetSchemeSupportsSignOutAsync(HttpContext context, string idp)
    {
        var provider = context.RequestServices.GetRequiredService<IAuthenticationHandlerProvider>();
        var handler = await provider.GetHandlerAsync(context, idp);
        return handler is IAuthenticationSignOutHandler;
    }
}
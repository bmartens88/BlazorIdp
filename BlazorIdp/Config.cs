using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using IdentityModel;

namespace BlazorIdp;

internal sealed class Config
{
    internal static IEnumerable<IdentityResource> IdentityResources =>
    [
        new IdentityResources.OpenId(),
        new IdentityResources.Profile(),
        new()
        {
            Name = "verification",
            UserClaims =
            [
                JwtClaimTypes.Email,
                JwtClaimTypes.EmailVerified
            ]
        },
        new("color", ["favorite_color"])
    ];

    internal static IEnumerable<ApiScope> ApiScopes =>
    [
        new("api1", "My API")
    ];

    internal static IEnumerable<Client> Clients =>
    [
        new()
        {
            ClientId = "web",
            ClientSecrets = [new Secret("secret".Sha256())],
            RequirePkce = true,
            RedirectUris = ["https://localhost:5001/signin-oidc"],
            PostLogoutRedirectUris = ["https://localhost:5001/signout-callback-oidc"],
            AllowedScopes =
            [
                IdentityServerConstants.StandardScopes.OpenId,
                IdentityServerConstants.StandardScopes.Profile,
                "verification",
                "api1",
                "color"
            ],
            AllowedGrantTypes = GrantTypes.Code
        }
    ];
}
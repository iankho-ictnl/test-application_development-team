using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using PropertyAdmin.Contracts;

namespace PropertyAdmin.Api.Auth;

public static class Policies
{
    public const string Read = "Read";

    public static void Configure(AuthorizationOptions options)
    {
        foreach (var p in Permissions.All)
            options.AddPolicy(p, b => b.RequireAuthenticatedUser().RequireClaim(Permissions.ClaimType, p));

        options.AddPolicy(Read, b => b.RequireAuthenticatedUser()
            .RequireClaim(Permissions.ClaimType, Permissions.ReadAll, Permissions.ReadOwn));

        options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    }
}

public static class ClaimsPrincipalExtensions
{
    public static string? UserId(this ClaimsPrincipal user) => user.FindFirstValue(JwtRegisteredClaimNames.Sub);

    public static bool Has(this ClaimsPrincipal user, string permission) =>
        user.HasClaim(Permissions.ClaimType, permission);

    /// <summary>
    /// Null when the user may read all owners; otherwise the only owner id the user may read
    /// (-1 when the user has no owner link, so nothing matches).
    /// </summary>
    public static int? ReadScopeOwnerId(this ClaimsPrincipal user)
    {
        if (user.Has(Permissions.ReadAll)) return null;
        return int.TryParse(user.FindFirstValue(AppClaims.OwnerId), out var id) ? id : -1;
    }
}

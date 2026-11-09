using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PropertyAdmin.Contracts;

namespace test_application_for_development_team.Services;

public static class AuthConstants
{
    public const string AccessTokenClaim = "access_token";
    public const string OwnerIdClaim = "owner_id";
}

public static class FrontendPolicies
{
    public const string Read = "Read";

    public static void Configure(AuthorizationOptions options)
    {
        foreach (var p in Permissions.All)
            options.AddPolicy(p, b => b.RequireAuthenticatedUser().RequireClaim(Permissions.ClaimType, p));
        options.AddPolicy(Read, b => b.RequireAuthenticatedUser().RequireClaim(Permissions.ClaimType, Permissions.ReadAll));
        options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    }
}

public static class UserExtensions
{
    public static bool Has(this ClaimsPrincipal user, string permission) => user.HasClaim(Permissions.ClaimType, permission);
}

public class SignInService(IHttpContextAccessor accessor)
{
    public async Task SignInAsync(LoginResponse response)
    {
        var jwt = new JwtSecurityTokenHandler { MapInboundClaims = false }.ReadJwtToken(response.AccessToken);
        var claims = new List<Claim> { new(AuthConstants.AccessTokenClaim, response.AccessToken!) };
        foreach (var c in jwt.Claims)
        {
            switch (c.Type)
            {
                case ClaimTypes.Name: claims.Add(new Claim(ClaimTypes.Name, c.Value)); break;
                case ClaimTypes.Role: claims.Add(new Claim(ClaimTypes.Role, c.Value)); break;
                case Permissions.ClaimType: claims.Add(new Claim(Permissions.ClaimType, c.Value)); break;
                case AuthConstants.OwnerIdClaim: claims.Add(new Claim(AuthConstants.OwnerIdClaim, c.Value)); break;
                case JwtRegisteredClaimNames.Sub: claims.Add(new Claim(ClaimTypes.NameIdentifier, c.Value)); break;
            }
        }
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
        await accessor.HttpContext!.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = false, ExpiresUtc = response.ExpiresUtc, AllowRefresh = false });
    }

    public Task SignOutAsync() => accessor.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
}

/// <summary>Turns API failures into the right page response.</summary>
public class ApiExceptionPageFilter(SignInService signIn) : IAsyncPageFilter
{
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var executed = await next();
        switch (executed.Exception)
        {
            case ApiUnauthorizedException:
                await signIn.SignOutAsync();
                executed.Result = new RedirectToPageResult("/Account/Login", new { returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString });
                executed.ExceptionHandled = true;
                break;
            case KeyNotFoundException:
                executed.Result = new NotFoundResult();
                executed.ExceptionHandled = true;
                break;
            case UnauthorizedAccessException:
                executed.Result = new RedirectToPageResult("/Account/AccessDenied");
                executed.ExceptionHandled = true;
                break;
        }
    }
}

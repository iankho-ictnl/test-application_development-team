using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using test_application_for_development_team.Services;

namespace test_application_for_development_team.Pages.Account;

/// <summary>
/// Signs the user out (S-26, D-036): first the API renews the user's security stamp with
/// POST api/auth/logout, so the token in the cookie is refused from then on, then the cookie goes.
/// The cookie always goes, also when the token was already refused (401) or the API cannot be reached.
/// </summary>
public partial class LogoutModel(ApiClient api, SignInService signIn, ILogger<LogoutModel> logger) : PageModel
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Sign-out: the API answered {Status} to auth/logout; the cookie is removed anyway.")]
    private static partial void LogLogoutRefused(ILogger logger, int status);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Sign-out: the API could not be reached for auth/logout; the cookie is removed anyway.")]
    private static partial void LogLogoutUnreachable(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Sign-out: auth/logout timed out; the cookie is removed anyway.")]
    private static partial void LogLogoutTimedOut(ILogger logger, Exception exception);

    public IActionResult OnGet() => RedirectToPage("/Index");

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var r = await api.PostAsync("api/auth/logout", null);
            if (!r.Ok && r.Status != System.Net.HttpStatusCode.Unauthorized)
                LogLogoutRefused(logger, (int)r.Status);
        }
        catch (ApiUnauthorizedException)
        {
            // The token is no longer valid: nothing left to end at the API.
        }
        catch (HttpRequestException ex)
        {
            LogLogoutUnreachable(logger, ex);
        }
        catch (TaskCanceledException ex) when (!HttpContext.RequestAborted.IsCancellationRequested)
        {
            LogLogoutTimedOut(logger, ex);
        }
        await signIn.SignOutAsync();
        return RedirectToPage("Login");
    }
}

public class CultureModel(Microsoft.Extensions.Options.IOptions<RequestLocalizationOptions> options) : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Index");

    public IActionResult OnPost(string culture, string? returnUrl)
    {
        if (options.Value.SupportedUICultures!.Any(c => c.Name == culture))
        {
            Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, SameSite = SameSiteMode.Lax, HttpOnly = true, Secure = Request.IsHttps });
        }
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
    }
}

public class AccessDeniedModel : PageModel
{
    public void OnGet() { }
}

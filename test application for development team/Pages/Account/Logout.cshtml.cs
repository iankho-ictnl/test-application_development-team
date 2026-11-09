using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using test_application_for_development_team.Services;

namespace test_application_for_development_team.Pages.Account;

public class LogoutModel(SignInService signIn) : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Index");

    public async Task<IActionResult> OnPostAsync()
    {
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

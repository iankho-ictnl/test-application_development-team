using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using PropertyAdmin.Contracts;
using test_application_for_development_team.Services;

namespace test_application_for_development_team.Pages.Account;

public class LoginTwoFactorModel(ApiClient api, SignInService signIn, IStringLocalizer<SharedResource> L) : PageModel
{
    [BindProperty, Required(ErrorMessage = "Required"), Display(Name = "Authentication code or recovery code")]
    public string Code { get; set; } = "";

    [BindProperty]
    public new string? Challenge { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public IActionResult OnGet()
    {
        Challenge = TempData["Challenge"] as string;
        return Challenge is null ? RedirectToPage("Login") : Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid || Challenge is null) return Page();
        var result = await api.PostAsync<LoginResponse>("api/auth/login-2fa", new TwoFactorLoginRequest(Challenge, Code));
        if (!result.Ok || result.Value?.AccessToken is null)
        {
            TempData["Error"] = L["The code is invalid or has expired. Sign in again."].Value;
            return RedirectToPage("Login", new { ReturnUrl });
        }
        await signIn.SignInAsync(result.Value);
        return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/");
    }
}

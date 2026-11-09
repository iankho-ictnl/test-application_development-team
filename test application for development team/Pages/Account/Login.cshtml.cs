using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using PropertyAdmin.Contracts;
using test_application_for_development_team.Services;

namespace test_application_for_development_team.Pages.Account;

public class LoginModel(ApiClient api, SignInService signIn, IStringLocalizer<SharedResource> L) : PageModel
{
    [BindProperty, Required(ErrorMessage = "Required"), Display(Name = "User name")]
    public string UserName { get; set; } = "";

    [BindProperty, Required(ErrorMessage = "Required"), DataType(DataType.Password), Display(Name = "Password")]
    public string Password { get; set; } = "";

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var result = await api.PostAsync<LoginResponse>("api/auth/login", new LoginRequest(UserName, Password));
        if (!result.Ok || result.Value is null)
        {
            ModelState.AddModelError("", L["Invalid user name or password, or the account is locked."]);
            return Page();
        }
        if (result.Value.RequiresTwoFactor)
        {
            TempData["Challenge"] = result.Value.ChallengeToken;
            return RedirectToPage("LoginTwoFactor", new { ReturnUrl });
        }
        await signIn.SignInAsync(result.Value);
        return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/");
    }
}

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using PropertyAdmin.Contracts;
using QRCoder;
using test_application_for_development_team.Services;

namespace test_application_for_development_team.Pages;

public class ProfileModel(ApiClient api, SignInService signIn, IStringLocalizer<SharedResource> L) : PageModel
{
    public TwoFactorSetupResponse? Setup { get; set; }
    public string? QrCodeDataUri { get; set; }
    public IReadOnlyList<string>? RecoveryCodes { get; set; }

    [BindProperty, Display(Name = "Authentication code")]
    public string? Code { get; set; }

    [BindProperty, DataType(DataType.Password), Display(Name = "Current password")]
    public string? CurrentPassword { get; set; }

    [BindProperty, DataType(DataType.Password), Display(Name = "New password")]
    public string? NewPassword { get; set; }

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostEnableAsync()
    {
        var r = await api.PostAsync<TwoFactorEnableResponse>("api/auth/2fa/enable", new TwoFactorEnableRequest(Code ?? ""));
        if (!r.Ok)
        {
            ModelState.AddModelError("", r.Error ?? L["The code is invalid."]);
            await LoadAsync();
            return Page();
        }
        RecoveryCodes = r.Value!.RecoveryCodes;
        await signIn.SignOutAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDisableAsync()
    {
        await api.PostAsync("api/auth/2fa/disable", null);
        await signIn.SignOutAsync();
        TempData["Error"] = L["Two-factor sign-in is turned off. Sign in again."].Value;
        return RedirectToPage("/Account/Login");
    }

    public async Task<IActionResult> OnPostPasswordAsync()
    {
        var r = await api.PostAsync("api/auth/password", new ChangePasswordRequest(CurrentPassword ?? "", NewPassword ?? ""));
        if (!r.Ok)
        {
            ModelState.AddModelError("", r.Error ?? "");
            await LoadAsync();
            return Page();
        }
        await signIn.SignOutAsync();
        TempData["Error"] = L["Your password has been changed. Sign in again."].Value;
        return RedirectToPage("/Account/Login");
    }

    private async Task LoadAsync()
    {
        Setup = await api.GetAsync<TwoFactorSetupResponse>("api/auth/2fa");
        if (!Setup.Enabled)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(Setup.AuthenticatorUri, QRCodeGenerator.ECCLevel.Q);
            var png = new PngByteQRCode(data).GetGraphic(5);
            QrCodeDataUri = "data:image/png;base64," + Convert.ToBase64String(png);
        }
    }
}


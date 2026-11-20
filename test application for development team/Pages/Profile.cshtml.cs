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

    [BindProperty, DataType(DataType.Password), Display(Name = "Current password"), StringLength(128, ErrorMessage = "Too long")]
    public string? DisablePassword { get; set; }

    [BindProperty, Display(Name = "Or a code from your authenticator app"), StringLength(64, ErrorMessage = "Too long")]
    public string? DisableCode { get; set; }

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
        // S-19: turning off the second factor needs the current password or an authenticator code (D-036).
        if (string.IsNullOrEmpty(DisablePassword) && string.IsNullOrWhiteSpace(DisableCode))
            ModelState.AddModelError(nameof(DisablePassword), L["Enter your current password or a code from your authenticator app."]);
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        var r = await api.PostAsync("api/auth/2fa/disable",
            new TwoFactorDisableRequest(NullIfEmpty(DisablePassword), NullIfEmpty(DisableCode?.Trim())));
        if (!r.Ok)
        {
            ModelState.AddModelError(nameof(DisablePassword),
                r.Status == System.Net.HttpStatusCode.BadRequest ? L["The password or code is incorrect."] : r.Error ?? L["Something went wrong."]);
            await LoadAsync();
            return Page();
        }
        await signIn.SignOutAsync();
        TempData["Error"] = L["Two-factor sign-in is turned off. Sign in again."].Value;
        return RedirectToPage("/Account/Login");
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;

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
        if (!Setup.Enabled && !string.IsNullOrEmpty(Setup.AuthenticatorUri))
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(Setup.AuthenticatorUri, QRCodeGenerator.ECCLevel.Q);
            var png = new PngByteQRCode(data).GetGraphic(5);
            QrCodeDataUri = "data:image/png;base64," + Convert.ToBase64String(png);
        }
    }
}


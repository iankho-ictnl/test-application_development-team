using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PropertyAdmin.Api.Auth;
using PropertyAdmin.Contracts;
using PropertyAdmin.Data;

namespace PropertyAdmin.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(UserManager<ApplicationUser> users, TokenService tokens) : ControllerBase
{
    private const string Issuer = "PropertyAdmin";

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var user = await users.FindByNameAsync(request.UserName);
        if (user is null) return Unauthorized();
        if (await users.IsLockedOutAsync(user)) return Unauthorized();
        if (!await users.CheckPasswordAsync(user, request.Password))
        {
            await users.AccessFailedAsync(user);
            return Unauthorized();
        }
        await users.ResetAccessFailedCountAsync(user);
        if (await users.GetTwoFactorEnabledAsync(user))
            return tokens.CreateChallengeToken(user);
        return await tokens.CreateAccessTokenAsync(user);
    }

    [AllowAnonymous]
    [HttpPost("login-2fa")]
    public async Task<ActionResult<LoginResponse>> LoginTwoFactor(TwoFactorLoginRequest request, CancellationToken ct)
    {
        var userId = tokens.ConsumeChallengeToken(request.ChallengeToken);
        var user = userId is null ? null : await users.FindByIdAsync(userId);
        if (user is null || await users.IsLockedOutAsync(user)) return Unauthorized();

        var code = request.Code.Replace(" ", "").Replace("-", "");
        var ok = await users.VerifyTwoFactorTokenAsync(user, users.Options.Tokens.AuthenticatorTokenProvider, code)
                 || (await users.RedeemTwoFactorRecoveryCodeAsync(user, request.Code.Trim())).Succeeded;
        if (!ok)
        {
            await users.AccessFailedAsync(user);
            return Unauthorized();
        }
        await users.ResetAccessFailedCountAsync(user);
        return await tokens.CreateAccessTokenAsync(user);
    }

    [HttpGet("2fa")]
    public async Task<ActionResult<TwoFactorSetupResponse>> GetTwoFactor(CancellationToken ct)
    {
        var user = await CurrentAsync();
        if (user is null) return Unauthorized();
        var key = await users.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await users.ResetAuthenticatorKeyAsync(user);
            key = await users.GetAuthenticatorKeyAsync(user);
        }
        var uri = $"otpauth://totp/{UrlEncoder.Default.Encode(Issuer)}:{UrlEncoder.Default.Encode(user.UserName!)}?secret={key}&issuer={UrlEncoder.Default.Encode(Issuer)}&digits=6";
        return new TwoFactorSetupResponse(key!, uri, user.TwoFactorEnabled);
    }

    [HttpPost("2fa/enable")]
    public async Task<ActionResult<TwoFactorEnableResponse>> EnableTwoFactor(TwoFactorEnableRequest request, CancellationToken ct)
    {
        var user = await CurrentAsync();
        if (user is null) return Unauthorized();
        var code = request.Code.Replace(" ", "").Replace("-", "");
        if (!await users.VerifyTwoFactorTokenAsync(user, users.Options.Tokens.AuthenticatorTokenProvider, code))
            return Problem("De code is ongeldig.", statusCode: StatusCodes.Status400BadRequest, title: "Ongeldige aanvraag");
        await users.SetTwoFactorEnabledAsync(user, true);
        var codes = await users.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        return new TwoFactorEnableResponse(codes!.ToList());
    }

    [HttpPost("2fa/disable")]
    public async Task<IActionResult> DisableTwoFactor(CancellationToken ct)
    {
        var user = await CurrentAsync();
        if (user is null) return Unauthorized();
        await users.SetTwoFactorEnabledAsync(user, false);
        await users.ResetAuthenticatorKeyAsync(user);
        return NoContent();
    }

    [HttpPost("password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        var user = await CurrentAsync();
        if (user is null) return Unauthorized();
        var result = await users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            return Problem(string.Join(" ", result.Errors.Select(e => e.Description)), statusCode: StatusCodes.Status400BadRequest, title: "Ongeldige aanvraag");
        return NoContent();
    }

    private Task<ApplicationUser?> CurrentAsync() => users.FindByIdAsync(User.UserId() ?? "");
}

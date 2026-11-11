using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using PropertyAdmin.Data;
using PropertyAdmin.Logic.V1.Auth.Models;

namespace PropertyAdmin.Logic.V1.Auth;

public sealed class AuthService(
    UserManager<ApplicationUser> users,
    RoleManager<IdentityRole> roles,
    ITokenIssuer tokens) : IAuthService
{
    private const string Issuer = "PropertyAdmin";
    public const string ReauthenticationRequired = "Geef je huidige wachtwoord of een code uit je authenticator-app.";
    public const string ReauthenticationFailed = "Het wachtwoord of de code is onjuist.";

    // Identity's UserManager takes no cancellation token, so each step checks it before it starts.
    public async Task<LoginModel?> LoginAsync(string userName, string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await users.FindByNameAsync(userName);
        if (user is null || await users.IsLockedOutAsync(user)) return null;
        if (!await users.CheckPasswordAsync(user, password))
        {
            await users.AccessFailedAsync(user);
            return null;
        }
        await users.ResetAccessFailedCountAsync(user);
        if (await users.GetTwoFactorEnabledAsync(user))
            return LoginModel.Challenge(tokens.CreateChallengeToken(user.Id, user.SecurityStamp ?? ""));
        return LoginModel.Access(tokens.CreateAccessToken(await CreateSubjectAsync(user, cancellationToken)));
    }

    public async Task<LoginModel?> LoginTwoFactorAsync(string challengeToken, string code, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userId = tokens.ConsumeChallengeToken(challengeToken);
        var user = userId is null ? null : await users.FindByIdAsync(userId);
        if (user is null || await users.IsLockedOutAsync(user)) return null;

        var ok = await users.VerifyTwoFactorTokenAsync(user, users.Options.Tokens.AuthenticatorTokenProvider, NormalizeCode(code))
                 || (await users.RedeemTwoFactorRecoveryCodeAsync(user, code.Trim())).Succeeded;
        if (!ok)
        {
            await users.AccessFailedAsync(user);
            return null;
        }
        await users.ResetAccessFailedCountAsync(user);
        return LoginModel.Access(tokens.CreateAccessToken(await CreateSubjectAsync(user, cancellationToken)));
    }

    public async Task<TwoFactorSetupModel?> GetTwoFactorAsync(string userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await users.FindByIdAsync(userId);
        if (user is null) return null;
        // S-18: once two-factor sign-in is on, the shared key is never handed out again.
        if (await users.GetTwoFactorEnabledAsync(user)) return new TwoFactorSetupModel(null, null, true);
        var key = await users.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await users.ResetAuthenticatorKeyAsync(user);
            key = await users.GetAuthenticatorKeyAsync(user);
        }
        var issuer = UrlEncoder.Default.Encode(Issuer);
        var uri = $"otpauth://totp/{issuer}:{UrlEncoder.Default.Encode(user.UserName!)}?secret={key}&issuer={issuer}&digits=6";
        return new TwoFactorSetupModel(key!, uri, false);
    }

    public async Task<IReadOnlyList<string>?> EnableTwoFactorAsync(string userId, string code, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await users.FindByIdAsync(userId);
        if (user is null) return null;
        if (!await users.VerifyTwoFactorTokenAsync(user, users.Options.Tokens.AuthenticatorTokenProvider, NormalizeCode(code)))
            throw new BusinessRuleException("De code is ongeldig.");
        await users.SetTwoFactorEnabledAsync(user, true);
        var codes = await users.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        return codes!.ToList();
    }

    public async Task<bool> DisableTwoFactorAsync(string userId, string? password, string? code, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await users.FindByIdAsync(userId);
        if (user is null || await users.IsLockedOutAsync(user)) return false;
        var hasPassword = !string.IsNullOrEmpty(password);
        var hasCode = !string.IsNullOrWhiteSpace(code);
        if (!hasPassword && !hasCode) throw new BusinessRuleException(ReauthenticationRequired);

        cancellationToken.ThrowIfCancellationRequested();
        // S-19: every credential given must be right, and one message for both does not tell which failed.
        var ok = (!hasPassword || await users.CheckPasswordAsync(user, password!))
                 && (!hasCode || await users.VerifyTwoFactorTokenAsync(user, users.Options.Tokens.AuthenticatorTokenProvider, NormalizeCode(code!)));
        if (!ok)
        {
            await users.AccessFailedAsync(user);
            throw new BusinessRuleException(ReauthenticationFailed);
        }
        await users.ResetAccessFailedCountAsync(user);
        if (!await users.GetTwoFactorEnabledAsync(user)) return true;
        await users.SetTwoFactorEnabledAsync(user, false);
        await users.ResetAuthenticatorKeyAsync(user);
        return true;
    }

    public async Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await users.FindByIdAsync(userId);
        if (user is null) return false;
        var result = await users.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
            throw new BusinessRuleException(string.Join(" ", result.Errors.Select(error => error.Description)));
        return true;
    }

    private async Task<TokenSubjectModel> CreateSubjectAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roleNames = await users.GetRolesAsync(user);
        var permissions = new HashSet<string>();
        foreach (var roleName in roleNames)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var role = await roles.FindByNameAsync(roleName);
            if (role is null) continue;
            foreach (var claim in await roles.GetClaimsAsync(role))
                if (claim.Type == Permissions.ClaimType) permissions.Add(claim.Value);
        }
        return new TokenSubjectModel(user.Id, user.UserName ?? "", user.SecurityStamp ?? "", user.OwnerId, roleNames.ToList(), permissions);
    }

    private static string NormalizeCode(string code) => code.Replace(" ", "").Replace("-", "");
}

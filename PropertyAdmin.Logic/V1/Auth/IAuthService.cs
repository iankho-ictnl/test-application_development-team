using PropertyAdmin.Logic.V1.Auth.Models;

namespace PropertyAdmin.Logic.V1.Auth;

/// <summary>Sign-in and the caller's own account. A null or false result means: not signed in (401).</summary>
public interface IAuthService
{
    Task<LoginModel?> LoginAsync(string userName, string password, CancellationToken cancellationToken);

    Task<LoginModel?> LoginTwoFactorAsync(string challengeToken, string code, CancellationToken cancellationToken);

    Task<TwoFactorSetupModel?> GetTwoFactorAsync(string userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>?> EnableTwoFactorAsync(string userId, string code, CancellationToken cancellationToken);

    Task<bool> DisableTwoFactorAsync(string userId, CancellationToken cancellationToken);

    Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken);
}

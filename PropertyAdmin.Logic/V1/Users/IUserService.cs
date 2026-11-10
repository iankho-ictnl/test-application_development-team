using PropertyAdmin.Logic.V1.Users.Models;

namespace PropertyAdmin.Logic.V1.Users;

/// <summary>User management. A null or false result means: the user does not exist (404).</summary>
public interface IUserService
{
    Task<IReadOnlyList<UserModel>> GetAllAsync(CancellationToken cancellationToken);

    Task<UserModel?> GetByIdAsync(string id, CancellationToken cancellationToken);

    Task<UserModel> CreateAsync(UserCreateModel model, CancellationToken cancellationToken);

    Task<UserModel?> UpdateAsync(string id, UserUpdateModel model, CancellationToken cancellationToken);

    Task<bool> ResetTwoFactorAsync(string id, CancellationToken cancellationToken);

    Task<bool> ForceSignOutAsync(string id, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken);
}

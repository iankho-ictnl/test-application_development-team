using PropertyAdmin.Logic.V1.Roles.Models;

namespace PropertyAdmin.Logic.V1.Roles;

/// <summary>Role management. A false result means: the role does not exist (404).</summary>
public interface IRoleService
{
    Task<IReadOnlyList<RoleModel>> GetAllAsync(CancellationToken cancellationToken);

    Task CreateAsync(RoleSaveModel model, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(string name, RoleSaveModel model, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(string name, CancellationToken cancellationToken);
}

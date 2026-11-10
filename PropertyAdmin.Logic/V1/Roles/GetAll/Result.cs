using PropertyAdmin.Logic.V1.Roles.Models;

namespace PropertyAdmin.Logic.V1.Roles.GetAll;

public sealed record Result(IReadOnlyList<RoleModel> Roles);

using PropertyAdmin.Logic.V1.Roles.Models;

namespace PropertyAdmin.Logic.V1.Roles.Update;

public sealed record Command(string Name, RoleSaveModel Role);

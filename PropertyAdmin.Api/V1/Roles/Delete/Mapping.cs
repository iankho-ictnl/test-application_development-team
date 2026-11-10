using PropertyAdmin.Logic.V1.Roles.Delete;

namespace PropertyAdmin.Api.V1.Roles.Delete;

public static class Mapping
{
    public static Command ToCommand(string name) => new(name);
}

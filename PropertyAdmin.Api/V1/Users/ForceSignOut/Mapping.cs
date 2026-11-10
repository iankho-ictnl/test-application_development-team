using PropertyAdmin.Logic.V1.Users.ForceSignOut;

namespace PropertyAdmin.Api.V1.Users.ForceSignOut;

public static class Mapping
{
    public static Command ToCommand(string id) => new(id);
}

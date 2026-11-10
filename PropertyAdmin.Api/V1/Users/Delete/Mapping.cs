using PropertyAdmin.Logic.V1.Users.Delete;

namespace PropertyAdmin.Api.V1.Users.Delete;

public static class Mapping
{
    public static Command ToCommand(string id) => new(id);
}

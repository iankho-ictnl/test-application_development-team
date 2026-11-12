using PropertyAdmin.Logic.V1.Auth.Logout;

namespace PropertyAdmin.Api.V1.Auth.Logout;

public static class Mapping
{
    public static Command ToCommand(string userId) => new(userId);
}

using PropertyAdmin.Logic.V1.Users.ResetTwoFactor;

namespace PropertyAdmin.Api.V1.Users.ResetTwoFactor;

public static class Mapping
{
    public static Command ToCommand(string id) => new(id);
}

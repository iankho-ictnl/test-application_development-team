using PropertyAdmin.Logic.V1.Auth.DisableTwoFactor;

namespace PropertyAdmin.Api.V1.Auth.DisableTwoFactor;

public static class Mapping
{
    public static Command ToCommand(string userId) => new(userId);
}

using PropertyAdmin.Logic.V1.Auth.ChangePassword;

namespace PropertyAdmin.Api.V1.Auth.ChangePassword;

public static class Mapping
{
    public static Command ToCommand(this Request request, string userId) => new(userId, request.CurrentPassword, request.NewPassword);
}

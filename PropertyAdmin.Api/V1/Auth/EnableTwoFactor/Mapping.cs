using PropertyAdmin.Logic.V1.Auth.EnableTwoFactor;

namespace PropertyAdmin.Api.V1.Auth.EnableTwoFactor;

public static class Mapping
{
    public static Command ToCommand(this Request request, string userId) => new(userId, request.Code);

    public static Response ToResponse(this Result result) => new(result.RecoveryCodes!);
}

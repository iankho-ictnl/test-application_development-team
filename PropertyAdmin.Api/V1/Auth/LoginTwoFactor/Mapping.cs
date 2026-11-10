using PropertyAdmin.Logic.V1.Auth.LoginTwoFactor;

namespace PropertyAdmin.Api.V1.Auth.LoginTwoFactor;

public static class Mapping
{
    public static Command ToCommand(this Request request) => new(request.ChallengeToken, request.Code);

    public static Response ToResponse(this Result result)
    {
        var login = result.Login!;
        return new Response(login.RequiresTwoFactor, login.ChallengeToken, login.AccessToken, login.ExpiresUtc);
    }
}

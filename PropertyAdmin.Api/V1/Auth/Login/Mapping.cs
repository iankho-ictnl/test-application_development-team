using PropertyAdmin.Logic.V1.Auth.Login;

namespace PropertyAdmin.Api.V1.Auth.Login;

public static class Mapping
{
    public static Command ToCommand(this Request request) => new(request.UserName, request.Password);

    public static Response ToResponse(this Result result)
    {
        var login = result.Login!;
        return new Response(login.RequiresTwoFactor, login.ChallengeToken, login.AccessToken, login.ExpiresUtc);
    }
}

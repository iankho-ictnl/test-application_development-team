using PropertyAdmin.Logic.V1.Auth.GetTwoFactor;

namespace PropertyAdmin.Api.V1.Auth.GetTwoFactor;

public static class Mapping
{
    public static Query ToQuery(string userId) => new(userId);

    public static Response ToResponse(this Result result)
    {
        var setup = result.Setup!;
        return new Response(setup.SharedKey, setup.AuthenticatorUri, setup.Enabled);
    }
}

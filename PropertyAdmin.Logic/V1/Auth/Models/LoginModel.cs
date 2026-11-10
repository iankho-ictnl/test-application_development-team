namespace PropertyAdmin.Logic.V1.Auth.Models;

public sealed record LoginModel(bool RequiresTwoFactor, string? ChallengeToken, string? AccessToken, DateTime? ExpiresUtc)
{
    public static LoginModel Challenge(string challengeToken) => new(true, challengeToken, null, null);

    public static LoginModel Access(AccessTokenModel token) => new(false, null, token.Token, token.ExpiresUtc);
}

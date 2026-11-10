using PropertyAdmin.Logic.V1.Auth.Models;

namespace PropertyAdmin.Logic.V1.Auth;

/// <summary>Writes and checks the tokens; implemented by the API's authentication set-up.</summary>
public interface ITokenIssuer
{
    AccessTokenModel CreateAccessToken(TokenSubjectModel subject);

    string CreateChallengeToken(string userId, string securityStamp);

    /// <summary>Validates a two-factor challenge token once; returns the user id or null.</summary>
    string? ConsumeChallengeToken(string token);
}

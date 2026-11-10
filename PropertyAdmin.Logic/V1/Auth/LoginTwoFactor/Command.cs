namespace PropertyAdmin.Logic.V1.Auth.LoginTwoFactor;

public sealed record Command(string ChallengeToken, string Code);

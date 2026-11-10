namespace PropertyAdmin.Api.V1.Auth.LoginTwoFactor;

public sealed record Response(bool RequiresTwoFactor, string? ChallengeToken, string? AccessToken, DateTime? ExpiresUtc);

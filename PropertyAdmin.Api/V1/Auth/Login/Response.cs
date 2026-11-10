namespace PropertyAdmin.Api.V1.Auth.Login;

public sealed record Response(bool RequiresTwoFactor, string? ChallengeToken, string? AccessToken, DateTime? ExpiresUtc);

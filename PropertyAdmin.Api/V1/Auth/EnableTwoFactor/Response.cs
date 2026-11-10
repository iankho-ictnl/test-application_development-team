namespace PropertyAdmin.Api.V1.Auth.EnableTwoFactor;

public sealed record Response(IReadOnlyList<string> RecoveryCodes);

namespace PropertyAdmin.Logic.V1.Auth.EnableTwoFactor;

public sealed record Command(string UserId, string Code);

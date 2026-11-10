namespace PropertyAdmin.Logic.V1.Auth.DisableTwoFactor;

/// <summary>Found is false when the signed-in user no longer exists.</summary>
public sealed record Result(bool Found);

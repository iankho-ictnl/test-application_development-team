namespace PropertyAdmin.Logic.V1.Auth.EnableTwoFactor;

/// <summary>RecoveryCodes is null when the signed-in user no longer exists.</summary>
public sealed record Result(IReadOnlyList<string>? RecoveryCodes);

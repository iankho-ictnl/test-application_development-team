namespace PropertyAdmin.Logic.V1.Auth.DisableTwoFactor;

/// <summary>Password or Code (authenticator only) re-authenticates the caller; at least one is required.</summary>
public sealed record Command(string UserId, string? Password, string? Code);

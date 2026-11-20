namespace PropertyAdmin.Logic.V1.Auth.Models;

/// <summary>SharedKey and AuthenticatorUri are null while two-factor sign-in is on.</summary>
public sealed record TwoFactorSetupModel(string? SharedKey, string? AuthenticatorUri, bool Enabled);

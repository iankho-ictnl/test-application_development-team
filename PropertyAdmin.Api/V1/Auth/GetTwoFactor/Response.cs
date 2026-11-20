namespace PropertyAdmin.Api.V1.Auth.GetTwoFactor;

/// <summary>SharedKey and AuthenticatorUri are null while two-factor sign-in is on.</summary>
public sealed record Response(string? SharedKey, string? AuthenticatorUri, bool Enabled);

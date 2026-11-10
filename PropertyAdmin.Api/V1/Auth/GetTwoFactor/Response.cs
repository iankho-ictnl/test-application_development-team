namespace PropertyAdmin.Api.V1.Auth.GetTwoFactor;

public sealed record Response(string SharedKey, string AuthenticatorUri, bool Enabled);

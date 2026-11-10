namespace PropertyAdmin.Logic.V1.Auth.Models;

public sealed record TwoFactorSetupModel(string SharedKey, string AuthenticatorUri, bool Enabled);

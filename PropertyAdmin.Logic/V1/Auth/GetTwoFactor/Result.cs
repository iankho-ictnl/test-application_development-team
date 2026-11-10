using PropertyAdmin.Logic.V1.Auth.Models;

namespace PropertyAdmin.Logic.V1.Auth.GetTwoFactor;

/// <summary>Setup is null when the signed-in user no longer exists.</summary>
public sealed record Result(TwoFactorSetupModel? Setup);

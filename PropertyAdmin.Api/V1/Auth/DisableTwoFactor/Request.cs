using System.ComponentModel.DataAnnotations;

namespace PropertyAdmin.Api.V1.Auth.DisableTwoFactor;

/// <summary>The current password or a current authenticator code (no recovery code).</summary>
public sealed record Request([MaxLength(128)] string? Password, [MaxLength(64)] string? Code);

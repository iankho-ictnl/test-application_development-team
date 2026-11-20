using System.ComponentModel.DataAnnotations;

namespace PropertyAdmin.Api.V1.Auth.EnableTwoFactor;

public sealed record Request([Required, MaxLength(64)] string Code);

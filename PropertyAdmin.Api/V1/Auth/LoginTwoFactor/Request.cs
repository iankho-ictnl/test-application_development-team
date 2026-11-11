using System.ComponentModel.DataAnnotations;

namespace PropertyAdmin.Api.V1.Auth.LoginTwoFactor;

public sealed record Request([Required, MaxLength(4096)] string ChallengeToken, [Required, MaxLength(64)] string Code);

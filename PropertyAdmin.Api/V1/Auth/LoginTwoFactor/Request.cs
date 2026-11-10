using System.ComponentModel.DataAnnotations;

namespace PropertyAdmin.Api.V1.Auth.LoginTwoFactor;

public sealed record Request([Required] string ChallengeToken, [Required] string Code);

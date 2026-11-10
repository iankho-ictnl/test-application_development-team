using System.ComponentModel.DataAnnotations;

namespace PropertyAdmin.Api.V1.Auth.EnableTwoFactor;

public sealed record Request([Required] string Code);

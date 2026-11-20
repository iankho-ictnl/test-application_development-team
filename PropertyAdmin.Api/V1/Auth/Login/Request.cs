using System.ComponentModel.DataAnnotations;

namespace PropertyAdmin.Api.V1.Auth.Login;

public sealed record Request([Required, MaxLength(100)] string UserName, [Required, MaxLength(128)] string Password);

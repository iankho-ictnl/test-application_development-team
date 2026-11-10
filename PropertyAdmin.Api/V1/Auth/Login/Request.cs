using System.ComponentModel.DataAnnotations;

namespace PropertyAdmin.Api.V1.Auth.Login;

public sealed record Request([Required] string UserName, [Required] string Password);

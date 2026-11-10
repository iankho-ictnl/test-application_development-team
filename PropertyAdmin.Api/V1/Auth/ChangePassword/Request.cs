using System.ComponentModel.DataAnnotations;

namespace PropertyAdmin.Api.V1.Auth.ChangePassword;

public sealed record Request([Required] string CurrentPassword, [Required] string NewPassword);

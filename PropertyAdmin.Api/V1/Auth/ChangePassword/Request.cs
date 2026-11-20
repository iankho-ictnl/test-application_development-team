using System.ComponentModel.DataAnnotations;

namespace PropertyAdmin.Api.V1.Auth.ChangePassword;

public sealed record Request([Required, MaxLength(128)] string CurrentPassword, [Required, MaxLength(128)] string NewPassword);

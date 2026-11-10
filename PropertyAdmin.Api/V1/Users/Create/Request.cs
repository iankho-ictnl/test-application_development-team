using System.ComponentModel.DataAnnotations;

namespace PropertyAdmin.Api.V1.Users.Create;

public sealed record Request(
    [Required, MaxLength(100)] string UserName,
    [Required, EmailAddress] string Email,
    [Required] string Password,
    int? OwnerId,
    IReadOnlyList<string> Roles);

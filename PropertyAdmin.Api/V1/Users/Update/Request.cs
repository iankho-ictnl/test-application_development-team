using System.ComponentModel.DataAnnotations;

namespace PropertyAdmin.Api.V1.Users.Update;

public sealed record Request([Required, EmailAddress] string Email, int? OwnerId, IReadOnlyList<string> Roles, bool LockedOut);

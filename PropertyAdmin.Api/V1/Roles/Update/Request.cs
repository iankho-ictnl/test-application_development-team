using System.ComponentModel.DataAnnotations;

namespace PropertyAdmin.Api.V1.Roles.Update;

public sealed record Request([Required, MaxLength(50)] string Name, IReadOnlyList<string> Permissions);

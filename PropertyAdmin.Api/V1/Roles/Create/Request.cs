using System.ComponentModel.DataAnnotations;

namespace PropertyAdmin.Api.V1.Roles.Create;

public sealed record Request([Required, MaxLength(50)] string Name, IReadOnlyList<string> Permissions);

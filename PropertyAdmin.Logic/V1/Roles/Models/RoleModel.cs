namespace PropertyAdmin.Logic.V1.Roles.Models;

public sealed record RoleModel(string Name, IReadOnlyList<string> Permissions, int UserCount);

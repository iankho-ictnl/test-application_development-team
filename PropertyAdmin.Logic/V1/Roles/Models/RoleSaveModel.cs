namespace PropertyAdmin.Logic.V1.Roles.Models;

public sealed record RoleSaveModel(string Name, IReadOnlyList<string> Permissions);

namespace PropertyAdmin.Api.V1.Roles.GetAll;

public sealed record Response(string Name, IReadOnlyList<string> Permissions, int UserCount);

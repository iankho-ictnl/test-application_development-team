namespace PropertyAdmin.Api.V1.Users.Create;

public sealed record Response(string Id, string UserName, string Email, bool TwoFactorEnabled, bool LockedOut, int? OwnerId, IReadOnlyList<string> Roles);

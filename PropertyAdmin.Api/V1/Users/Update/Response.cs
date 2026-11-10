namespace PropertyAdmin.Api.V1.Users.Update;

public sealed record Response(string Id, string UserName, string Email, bool TwoFactorEnabled, bool LockedOut, int? OwnerId, IReadOnlyList<string> Roles);

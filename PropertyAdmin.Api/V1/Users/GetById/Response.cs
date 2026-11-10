namespace PropertyAdmin.Api.V1.Users.GetById;

public sealed record Response(string Id, string UserName, string Email, bool TwoFactorEnabled, bool LockedOut, int? OwnerId, IReadOnlyList<string> Roles);

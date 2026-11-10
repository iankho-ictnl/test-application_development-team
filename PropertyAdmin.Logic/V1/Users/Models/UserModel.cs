namespace PropertyAdmin.Logic.V1.Users.Models;

public sealed record UserModel(
    string Id,
    string UserName,
    string Email,
    bool TwoFactorEnabled,
    bool LockedOut,
    int? OwnerId,
    IReadOnlyList<string> Roles);

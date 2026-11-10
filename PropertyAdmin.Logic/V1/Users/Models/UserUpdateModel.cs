namespace PropertyAdmin.Logic.V1.Users.Models;

public sealed record UserUpdateModel(string Email, int? OwnerId, IReadOnlyList<string> Roles, bool LockedOut);

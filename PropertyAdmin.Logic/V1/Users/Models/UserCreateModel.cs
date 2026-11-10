namespace PropertyAdmin.Logic.V1.Users.Models;

public sealed record UserCreateModel(string UserName, string Email, string Password, int? OwnerId, IReadOnlyList<string> Roles);

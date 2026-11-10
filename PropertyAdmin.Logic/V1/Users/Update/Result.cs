using PropertyAdmin.Logic.V1.Users.Models;

namespace PropertyAdmin.Logic.V1.Users.Update;

/// <summary>User is null when the user does not exist.</summary>
public sealed record Result(UserModel? User);

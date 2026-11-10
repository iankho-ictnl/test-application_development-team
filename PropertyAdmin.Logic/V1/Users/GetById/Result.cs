using PropertyAdmin.Logic.V1.Users.Models;

namespace PropertyAdmin.Logic.V1.Users.GetById;

/// <summary>User is null when the user does not exist.</summary>
public sealed record Result(UserModel? User);

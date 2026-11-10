using PropertyAdmin.Logic.V1.Users.Models;

namespace PropertyAdmin.Logic.V1.Users.GetAll;

public sealed record Result(IReadOnlyList<UserModel> Users);

using PropertyAdmin.Logic.V1.Users.Models;

namespace PropertyAdmin.Logic.V1.Users.Create;

public sealed record Command(UserCreateModel User);

using PropertyAdmin.Logic.V1.Users.Models;

namespace PropertyAdmin.Logic.V1.Users.Update;

public sealed record Command(string Id, UserUpdateModel User);

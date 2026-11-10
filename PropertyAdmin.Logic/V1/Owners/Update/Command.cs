using PropertyAdmin.Logic.V1.Owners.Models;

namespace PropertyAdmin.Logic.V1.Owners.Update;

public sealed record Command(int Id, OwnerSaveModel Owner);
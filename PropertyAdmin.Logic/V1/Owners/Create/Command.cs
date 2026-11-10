using PropertyAdmin.Logic.V1.Owners.Models;

namespace PropertyAdmin.Logic.V1.Owners.Create;

public sealed record Command(OwnerSaveModel Owner);
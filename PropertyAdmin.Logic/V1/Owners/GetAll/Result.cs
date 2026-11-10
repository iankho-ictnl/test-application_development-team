using PropertyAdmin.Logic.V1.Owners.Models;

namespace PropertyAdmin.Logic.V1.Owners.GetAll;

public sealed record Result(IReadOnlyList<OwnerModel> Owners);
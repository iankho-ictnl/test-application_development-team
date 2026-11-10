using PropertyAdmin.Logic.V1.Assets.Models;

namespace PropertyAdmin.Logic.V1.Assets.GetAll;

public sealed record Result(IReadOnlyList<AssetModel> Assets);
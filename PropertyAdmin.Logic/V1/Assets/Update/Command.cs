using PropertyAdmin.Logic.V1.Assets.Models;

namespace PropertyAdmin.Logic.V1.Assets.Update;

public sealed record Command(int Id, AssetSaveModel Asset);
using PropertyAdmin.Logic.V1.Assets.Models;

namespace PropertyAdmin.Logic.V1.Assets.Create;

public sealed record Command(AssetSaveModel Asset);
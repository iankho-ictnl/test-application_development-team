using PropertyAdmin.Data;

namespace PropertyAdmin.Logic.V1.Assets.Models;

public sealed record AssetSaveModel(
    int OwnerId,
    AssetType Type,
    string Name,
    string Address,
    string PostalCode,
    string City);
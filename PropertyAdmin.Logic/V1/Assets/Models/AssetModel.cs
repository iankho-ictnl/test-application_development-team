using PropertyAdmin.Contracts;

namespace PropertyAdmin.Logic.V1.Assets.Models;

public sealed record AssetModel(
    int Id,
    int OwnerId,
    string OwnerName,
    AssetType Type,
    string Name,
    string Address,
    string PostalCode,
    string City);
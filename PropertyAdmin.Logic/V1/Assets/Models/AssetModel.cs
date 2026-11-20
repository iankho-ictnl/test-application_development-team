using PropertyAdmin.Data;

namespace PropertyAdmin.Logic.V1.Assets.Models;

public sealed record AssetModel(
    int Id,
    int OwnerId,
    string OwnerName,
    AssetType Type,
    string Name,
    string Street,
    string HouseNumber,
    string PostalCode,
    string City,
    string Country);
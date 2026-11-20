
namespace PropertyAdmin.Api.V1.Assets.GetAll;

public sealed record Response(int Id, int OwnerId, string OwnerName, AssetType Type, string Name, string Street, string HouseNumber, string PostalCode, string City, string Country);
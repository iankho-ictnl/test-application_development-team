using PropertyAdmin.Logic.V1.Owners;
using PropertyAdmin.Logic.V1.Owners.Models;
using PropertyAdmin.Tests.V1;

namespace PropertyAdmin.Tests.V1.Owners;

public sealed class OwnerServiceTests
{
    [Fact]
    public async Task GetAll_filters_by_owner_id_and_maps_owner_response()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var service = new OwnerService(database.Context);
        var first = await service.CreateAsync(new OwnerSaveModel(" First ", null, null, null, null, null), CancellationToken.None);
        await service.CreateAsync(new OwnerSaveModel("Second", null, null, null, null, null), CancellationToken.None);

        var result = await service.GetAllAsync(first.Id, CancellationToken.None);
        var response = PropertyAdmin.Api.V1.Owners.GetAll.Mapping.ToResponse(
            new PropertyAdmin.Logic.V1.Owners.GetAll.Result(result));

        var ownerResponse = Assert.Single(response);
        Assert.Equal(first.Id, ownerResponse.Id);
        Assert.Equal("First", ownerResponse.Name);
    }

    [Fact]
    public async Task Delete_rejects_an_owner_with_assets()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var service = new OwnerService(database.Context);
        var owner = await service.CreateAsync(new OwnerSaveModel("Owner", null, null, null, null, null), CancellationToken.None);
        database.Context.Assets.Add(new PropertyAdmin.Data.Asset
        {
            OwnerId = owner.Id,
            Name = "Asset",
            Address = "Street",
            PostalCode = "1000",
            City = "City",
        });
        await database.Context.SaveChangesAsync();

        await Assert.ThrowsAsync<PropertyAdmin.Logic.BusinessRuleException>(
            () => service.DeleteAsync(owner.Id, CancellationToken.None));
    }
}
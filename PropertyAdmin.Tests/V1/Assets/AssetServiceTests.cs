using PropertyAdmin.Contracts;
using PropertyAdmin.Data;
using PropertyAdmin.Logic.V1.Assets;
using PropertyAdmin.Logic.V1.Assets.Models;
using PropertyAdmin.Tests.V1;

namespace PropertyAdmin.Tests.V1.Assets;

public sealed class AssetServiceTests
{
    [Fact]
    public async Task Update_rejects_owner_change_while_uninvoiced_activity_exists()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var firstOwner = new Owner { Name = "First" };
        var secondOwner = new Owner { Name = "Second" };
        database.Context.Owners.AddRange(firstOwner, secondOwner);
        await database.Context.SaveChangesAsync();

        var service = new AssetService(database.Context);
        var asset = await service.CreateAsync(
            new AssetSaveModel(firstOwner.Id, AssetType.Building, "House", "Road", "1000", "City"),
            CancellationToken.None);
        database.Context.Activities.Add(new Activity
        {
            AssetId = asset.Id,
            Date = new DateOnly(2026, 1, 1),
            Description = "Work",
            VatRateName = "High",
        });
        await database.Context.SaveChangesAsync();

        await Assert.ThrowsAsync<PropertyAdmin.Logic.BusinessRuleException>(() => service.UpdateAsync(
            asset.Id,
            new AssetSaveModel(secondOwner.Id, AssetType.Home, "House", "Road", "1000", "City"),
            CancellationToken.None));
    }

    [Fact]
    public async Task GetAll_applies_owner_scope_and_maps_asset_response()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var owner = new Owner { Name = "Owner" };
        var otherOwner = new Owner { Name = "Other" };
        database.Context.Owners.AddRange(owner, otherOwner);
        await database.Context.SaveChangesAsync();
        var service = new AssetService(database.Context);
        await service.CreateAsync(new AssetSaveModel(owner.Id, AssetType.Building, "Mine", "Road", "1000", "City"), CancellationToken.None);
        await service.CreateAsync(new AssetSaveModel(otherOwner.Id, AssetType.Home, "Other", "Road", "1000", "City"), CancellationToken.None);

        var result = await service.GetAllAsync(owner.Id, null, CancellationToken.None);
        var response = PropertyAdmin.Api.V1.Assets.GetAll.Mapping.ToResponse(
            new PropertyAdmin.Logic.V1.Assets.GetAll.Result(result));

        var assetResponse = Assert.Single(response);
        Assert.Equal(owner.Id, assetResponse.OwnerId);
        Assert.Equal("Mine", assetResponse.Name);
    }
}
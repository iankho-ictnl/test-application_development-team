using PropertyAdmin.Data;
using PropertyAdmin.Logic.V1.Assets;
using PropertyAdmin.Logic.V1.Assets.Models;
using PropertyAdmin.Tests.V1;
using AssetsApi = PropertyAdmin.Api.V1.Assets;
using AssetsLogic = PropertyAdmin.Logic.V1.Assets;

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

    [Fact]
    public async Task Handlers_create_read_update_and_delete_within_the_owner_scope()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var owner = new Owner { Name = "Owner" };
        var otherOwner = new Owner { Name = "Other" };
        database.Context.Owners.AddRange(owner, otherOwner);
        await database.Context.SaveChangesAsync();
        var service = new AssetService(database.Context);
        var create = new AssetsLogic.Create.CreateHandler(service);
        var mine = (await create.HandleAsync(new(Save(owner.Id, "Mine")), CancellationToken.None)).Asset;
        var other = (await create.HandleAsync(new(Save(otherOwner.Id, "Other")), CancellationToken.None)).Asset;
        Assert.Equal("Owner", mine.OwnerName);

        var getAll = new AssetsLogic.GetAll.GetAllHandler(service);
        Assert.Equal([mine.Id, other.Id], (await getAll.HandleAsync(new(null, null), CancellationToken.None)).Assets.Select(asset => asset.Id).Order());
        Assert.Equal([mine.Id], (await getAll.HandleAsync(new(owner.Id, null), CancellationToken.None)).Assets.Select(asset => asset.Id));
        Assert.Equal([other.Id], (await getAll.HandleAsync(new(null, otherOwner.Id), CancellationToken.None)).Assets.Select(asset => asset.Id));

        var getById = new AssetsLogic.GetById.GetByIdHandler(service);
        Assert.Equal(mine, (await getById.HandleAsync(new(mine.Id, owner.Id), CancellationToken.None)).Asset);
        Assert.Null((await getById.HandleAsync(new(mine.Id, otherOwner.Id), CancellationToken.None)).Asset);

        var update = new AssetsLogic.Update.UpdateHandler(service);
        var updated = (await update.HandleAsync(new(mine.Id, Save(owner.Id, "Renamed") with { Type = AssetType.Home }), CancellationToken.None)).Asset!;
        Assert.Equal(("Renamed", AssetType.Home), (updated.Name, updated.Type));
        Assert.Null((await update.HandleAsync(new(mine.Id + 100, Save(owner.Id, "Unknown")), CancellationToken.None)).Asset);

        var delete = new AssetsLogic.Delete.DeleteHandler(service);
        Assert.True((await delete.HandleAsync(new(mine.Id), CancellationToken.None)).Deleted);
        Assert.False((await delete.HandleAsync(new(mine.Id), CancellationToken.None)).Deleted);
    }

    [Fact]
    public async Task Handlers_pass_a_cancellation_through()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var service = new AssetService(database.Context);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new AssetsLogic.GetAll.GetAllHandler(service).HandleAsync(new(null, null), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new AssetsLogic.GetById.GetByIdHandler(service).HandleAsync(new(1, null), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new AssetsLogic.Create.CreateHandler(service).HandleAsync(new(Save(1, "Asset")), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new AssetsLogic.Update.UpdateHandler(service).HandleAsync(new(1, Save(1, "Asset")), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new AssetsLogic.Delete.DeleteHandler(service).HandleAsync(new(1), cancellation.Token));
    }

    [Fact]
    public void Mappings_copy_every_field_and_convert_the_asset_type()
    {
        var asset = new AssetModel(7, 3, "Owner", AssetType.Home, "House", "Road 1", "1000 AA", "City");
        var save = new AssetSaveModel(3, AssetType.Home, "House", "Road 1", "1000 AA", "City");
        var expected = new { Id = 7, OwnerId = 3, OwnerName = "Owner", Type = PropertyAdmin.Api.V1.AssetType.Home, Name = "House", Address = "Road 1", PostalCode = "1000 AA", City = "City" };

        Assert.Equal(new AssetsLogic.GetAll.Query(5, 3), AssetsApi.GetAll.Mapping.ToQuery(new AssetsApi.GetAll.Request(3), 5));
        Assert.Equivalent(expected, Assert.Single(AssetsApi.GetAll.Mapping.ToResponse(new AssetsLogic.GetAll.Result([asset]))), strict: true);
        Assert.Equal(new AssetsLogic.GetById.Query(7, 5), AssetsApi.GetById.Mapping.ToQuery(new AssetsApi.GetById.Request(7), 5));
        Assert.Equivalent(expected, AssetsApi.GetById.Mapping.ToResponse(new AssetsLogic.GetById.Result(asset)), strict: true);

        Assert.Equal(new AssetsLogic.Create.Command(save), AssetsApi.Create.Mapping.ToCommand(
            new AssetsApi.Create.Request(3, PropertyAdmin.Api.V1.AssetType.Home, "House", "Road 1", "1000 AA", "City")));
        Assert.Equivalent(expected, AssetsApi.Create.Mapping.ToResponse(new AssetsLogic.Create.Result(asset)), strict: true);

        Assert.Equal(new AssetsLogic.Update.Command(7, save), AssetsApi.Update.Mapping.ToCommand(
            new AssetsApi.Update.Request(3, PropertyAdmin.Api.V1.AssetType.Home, "House", "Road 1", "1000 AA", "City"), 7));
        Assert.Equivalent(expected, AssetsApi.Update.Mapping.ToResponse(new AssetsLogic.Update.Result(asset)), strict: true);

        Assert.Equal(new AssetsLogic.Delete.Command(7), AssetsApi.Delete.Mapping.ToCommand(new AssetsApi.Delete.Request(7)));
        Assert.True(AssetsApi.Delete.Mapping.ToResponse(new AssetsLogic.Delete.Result(true)).Deleted);
    }

    private static AssetSaveModel Save(int ownerId, string name) =>
        new(ownerId, AssetType.Building, name, "Road", "1000", "City");
}
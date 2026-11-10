using PropertyAdmin.Logic.V1.Owners;
using PropertyAdmin.Logic.V1.Owners.Models;
using PropertyAdmin.Tests.V1;
using OwnersApi = PropertyAdmin.Api.V1.Owners;
using OwnersLogic = PropertyAdmin.Logic.V1.Owners;

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

    [Fact]
    public async Task Handlers_create_read_update_and_delete_within_the_owner_scope()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var service = new OwnerService(database.Context);
        var create = new OwnersLogic.Create.CreateHandler(service);
        var first = (await create.HandleAsync(new(Save("First")), CancellationToken.None)).Owner;
        var second = (await create.HandleAsync(new(Save("Second")), CancellationToken.None)).Owner;

        var getAll = new OwnersLogic.GetAll.GetAllHandler(service);
        Assert.Equal(["First", "Second"], (await getAll.HandleAsync(new(null), CancellationToken.None)).Owners.Select(owner => owner.Name));
        Assert.Equal([first.Id], (await getAll.HandleAsync(new(first.Id), CancellationToken.None)).Owners.Select(owner => owner.Id));

        var getById = new OwnersLogic.GetById.GetByIdHandler(service);
        Assert.Equal(first, (await getById.HandleAsync(new(first.Id, null), CancellationToken.None)).Owner);
        Assert.Null((await getById.HandleAsync(new(first.Id, second.Id), CancellationToken.None)).Owner);

        var update = new OwnersLogic.Update.UpdateHandler(service);
        Assert.Equal("Renamed", (await update.HandleAsync(new(first.Id, Save(" Renamed ")), CancellationToken.None)).Owner!.Name);
        Assert.Null((await update.HandleAsync(new(first.Id + 100, Save("Unknown")), CancellationToken.None)).Owner);

        var delete = new OwnersLogic.Delete.DeleteHandler(service);
        Assert.True((await delete.HandleAsync(new(first.Id), CancellationToken.None)).Deleted);
        Assert.False((await delete.HandleAsync(new(first.Id), CancellationToken.None)).Deleted);
    }

    [Fact]
    public async Task Handlers_pass_a_cancellation_through()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var service = new OwnerService(database.Context);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new OwnersLogic.GetAll.GetAllHandler(service).HandleAsync(new(null), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new OwnersLogic.GetById.GetByIdHandler(service).HandleAsync(new(1, null), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new OwnersLogic.Create.CreateHandler(service).HandleAsync(new(Save("Owner")), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new OwnersLogic.Update.UpdateHandler(service).HandleAsync(new(1, Save("Owner")), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new OwnersLogic.Delete.DeleteHandler(service).HandleAsync(new(1), cancellation.Token));
    }

    [Fact]
    public void Mappings_copy_every_field()
    {
        var owner = new OwnerModel(7, "Name", "owner@example.test", "0612345678", "Road 1", "1000 AA", "City");
        var save = new OwnerSaveModel("Name", "owner@example.test", "0612345678", "Road 1", "1000 AA", "City");

        Assert.Equal(new OwnersLogic.GetAll.Query(5), OwnersApi.GetAll.Mapping.ToQuery(new OwnersApi.GetAll.Request(9), 5));
        Assert.Equal(new OwnersLogic.GetAll.Query(9), OwnersApi.GetAll.Mapping.ToQuery(new OwnersApi.GetAll.Request(9), null));
        Assert.Equal(new OwnersLogic.GetById.Query(7, 5), OwnersApi.GetById.Mapping.ToQuery(new OwnersApi.GetById.Request(7), 5));
        Assert.Equivalent(owner, OwnersApi.GetById.Mapping.ToResponse(new OwnersLogic.GetById.Result(owner)), strict: true);

        Assert.Equal(new OwnersLogic.Create.Command(save), OwnersApi.Create.Mapping.ToCommand(
            new OwnersApi.Create.Request("Name", "owner@example.test", "0612345678", "Road 1", "1000 AA", "City")));
        Assert.Equivalent(owner, OwnersApi.Create.Mapping.ToResponse(new OwnersLogic.Create.Result(owner)), strict: true);

        Assert.Equal(new OwnersLogic.Update.Command(7, save), OwnersApi.Update.Mapping.ToCommand(
            new OwnersApi.Update.Request("Name", "owner@example.test", "0612345678", "Road 1", "1000 AA", "City"), 7));
        Assert.Equivalent(owner, OwnersApi.Update.Mapping.ToResponse(new OwnersLogic.Update.Result(owner)), strict: true);

        Assert.Equal(new OwnersLogic.Delete.Command(7), OwnersApi.Delete.Mapping.ToCommand(new OwnersApi.Delete.Request(7)));
        Assert.True(OwnersApi.Delete.Mapping.ToResponse(new OwnersLogic.Delete.Result(true)).Deleted);
    }

    private static OwnerSaveModel Save(string name) => new(name, null, null, null, null, null);
}
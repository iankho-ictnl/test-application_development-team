using Microsoft.EntityFrameworkCore;
using PropertyAdmin.Data;
using PropertyAdmin.Logic.V1.Assets.Models;

namespace PropertyAdmin.Logic.V1.Assets;

public sealed class AssetService(AppDbContext db) : IAssetService
{
    public async Task<IReadOnlyList<AssetModel>> GetAllAsync(int? ownerScopeId, int? ownerId, CancellationToken cancellationToken) =>
        await db.Assets.AsNoTracking()
            .Where(asset => (ownerScopeId == null || asset.OwnerId == ownerScopeId)
                && (ownerId == null || asset.OwnerId == ownerId))
            .OrderBy(asset => asset.Name)
            .Select(asset => new AssetModel(asset.Id, asset.OwnerId, asset.Owner.Name, asset.Type, asset.Name, asset.Address, asset.PostalCode, asset.City))
            .ToListAsync(cancellationToken);

    public async Task<AssetModel?> GetByIdAsync(int id, int? ownerScopeId, CancellationToken cancellationToken) =>
        await db.Assets.AsNoTracking()
            .Where(asset => asset.Id == id && (ownerScopeId == null || asset.OwnerId == ownerScopeId))
            .Select(asset => new AssetModel(asset.Id, asset.OwnerId, asset.Owner.Name, asset.Type, asset.Name, asset.Address, asset.PostalCode, asset.City))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<AssetModel> CreateAsync(AssetSaveModel model, CancellationToken cancellationToken)
    {
        await EnsureOwnerAsync(model.OwnerId, cancellationToken);
        var asset = new Asset();
        Apply(asset, model);
        db.Assets.Add(asset);
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(asset).Reference(item => item.Owner).LoadAsync(cancellationToken);
        return ToModel(asset);
    }

    public async Task<AssetModel?> UpdateAsync(int id, AssetSaveModel model, CancellationToken cancellationToken)
    {
        var asset = await db.Assets.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (asset is null) return null;

        await EnsureOwnerAsync(model.OwnerId, cancellationToken);
        if (asset.OwnerId != model.OwnerId
            && (await db.Activities.AnyAsync(item => item.AssetId == id && item.InvoiceId == null, cancellationToken)
                || await db.Expenses.AnyAsync(item => item.AssetId == id && item.InvoiceId == null && item.Reinvoiceable, cancellationToken)))
            throw new BusinessRuleException("Object heeft nog niet-gefactureerde regels; factureer die eerst voordat de eigenaar wijzigt.");

        Apply(asset, model);
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(asset).Reference(item => item.Owner).LoadAsync(cancellationToken);
        return ToModel(asset);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var asset = await db.Assets.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (asset is null) return false;

        if (await db.Activities.AnyAsync(item => item.AssetId == id, cancellationToken)
            || await db.Expenses.AnyAsync(item => item.AssetId == id, cancellationToken))
            throw new BusinessRuleException("Object heeft nog kosten of activiteiten en kan niet worden verwijderd.");

        db.Assets.Remove(asset);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsureOwnerAsync(int ownerId, CancellationToken cancellationToken)
    {
        if (!await db.Owners.AnyAsync(owner => owner.Id == ownerId, cancellationToken))
            throw new BusinessRuleException("Eigenaar bestaat niet.");
    }

    private static void Apply(Asset asset, AssetSaveModel model)
    {
        asset.OwnerId = model.OwnerId;
        asset.Type = model.Type;
        asset.Name = model.Name.Trim();
        asset.Address = model.Address;
        asset.PostalCode = model.PostalCode;
        asset.City = model.City;
    }

    private static AssetModel ToModel(Asset asset) =>
        new(asset.Id, asset.OwnerId, asset.Owner.Name, asset.Type, asset.Name, asset.Address, asset.PostalCode, asset.City);
}
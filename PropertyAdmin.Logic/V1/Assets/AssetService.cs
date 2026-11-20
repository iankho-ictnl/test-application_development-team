using Microsoft.EntityFrameworkCore;
using PropertyAdmin.Data;
using PropertyAdmin.Logic.V1.Assets.Models;

namespace PropertyAdmin.Logic.V1.Assets;

public sealed class AssetService(AppDbContext db) : IAssetService
{
    /// <summary>Business-rule messages as English keys; the Dutch text is in Resources/BusinessRuleMessages.nl.resx (D-050).</summary>
    public const string OwnerNotFound = "The owner does not exist.";
    public const string HasUninvoicedLines = "The asset still has uninvoiced lines; invoice them before changing the owner.";
    public const string HasLines = "The asset still has expenses or activities and cannot be deleted.";

    public async Task<IReadOnlyList<AssetModel>> GetAllAsync(int? ownerScopeId, int? ownerId, CancellationToken cancellationToken) =>
        await db.Assets.AsNoTracking()
            .Where(asset => (ownerScopeId == null || asset.OwnerId == ownerScopeId)
                && (ownerId == null || asset.OwnerId == ownerId))
            .OrderBy(asset => asset.Name)
            .Select(asset => new AssetModel(asset.Id, asset.OwnerId, asset.Owner.Name, asset.Type, asset.Name, asset.Street, asset.HouseNumber, asset.PostalCode, asset.City, asset.Country))
            .ToListAsync(cancellationToken);

    public async Task<AssetModel?> GetByIdAsync(int id, int? ownerScopeId, CancellationToken cancellationToken) =>
        await db.Assets.AsNoTracking()
            .Where(asset => asset.Id == id && (ownerScopeId == null || asset.OwnerId == ownerScopeId))
            .Select(asset => new AssetModel(asset.Id, asset.OwnerId, asset.Owner.Name, asset.Type, asset.Name, asset.Street, asset.HouseNumber, asset.PostalCode, asset.City, asset.Country))
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
            throw new BusinessRuleException(HasUninvoicedLines);

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
            throw new BusinessRuleException(HasLines);

        db.Assets.Remove(asset);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsureOwnerAsync(int ownerId, CancellationToken cancellationToken)
    {
        if (!await db.Owners.AnyAsync(owner => owner.Id == ownerId, cancellationToken))
            throw new BusinessRuleException(OwnerNotFound);
    }

    private static void Apply(Asset asset, AssetSaveModel model)
    {
        asset.OwnerId = model.OwnerId;
        asset.Type = model.Type;
        asset.Name = model.Name.Trim();
        asset.Street = model.Street.Trim();
        asset.HouseNumber = model.HouseNumber.Trim();
        asset.PostalCode = model.PostalCode.Trim();
        asset.City = model.City.Trim();
        asset.Country = model.Country.Trim();
    }

    private static AssetModel ToModel(Asset asset) =>
        new(asset.Id, asset.OwnerId, asset.Owner.Name, asset.Type, asset.Name, asset.Street, asset.HouseNumber, asset.PostalCode, asset.City, asset.Country);
}
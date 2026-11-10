using Microsoft.EntityFrameworkCore;
using PropertyAdmin.Data;
using PropertyAdmin.Logic.V1.Owners.Models;

namespace PropertyAdmin.Logic.V1.Owners;

public sealed class OwnerService(AppDbContext db) : IOwnerService
{
    public async Task<IReadOnlyList<OwnerModel>> GetAllAsync(int? ownerId, CancellationToken cancellationToken)
    {
        return await db.Owners.AsNoTracking()
            .Where(owner => ownerId == null || owner.Id == ownerId)
            .OrderBy(owner => owner.Name)
            .Select(owner => new OwnerModel(
                owner.Id,
                owner.Name,
                owner.Email,
                owner.Phone,
                owner.Address,
                owner.PostalCode,
                owner.City))
            .ToListAsync(cancellationToken);
    }

    public async Task<OwnerModel?> GetByIdAsync(int id, int? ownerId, CancellationToken cancellationToken)
    {
        return await db.Owners.AsNoTracking()
            .Where(owner => owner.Id == id && (ownerId == null || owner.Id == ownerId))
            .Select(owner => new OwnerModel(
                owner.Id,
                owner.Name,
                owner.Email,
                owner.Phone,
                owner.Address,
                owner.PostalCode,
                owner.City))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<OwnerModel> CreateAsync(OwnerSaveModel model, CancellationToken cancellationToken)
    {
        var owner = new Owner();
        Apply(owner, model);
        db.Owners.Add(owner);
        await db.SaveChangesAsync(cancellationToken);
        return ToModel(owner);
    }

    public async Task<OwnerModel?> UpdateAsync(int id, OwnerSaveModel model, CancellationToken cancellationToken)
    {
        var owner = await db.Owners.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (owner is null) return null;

        Apply(owner, model);
        await db.SaveChangesAsync(cancellationToken);
        return ToModel(owner);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var owner = await db.Owners.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (owner is null) return false;

        if (await db.Assets.AnyAsync(asset => asset.OwnerId == id, cancellationToken)
            || await db.Invoices.AnyAsync(invoice => invoice.OwnerId == id, cancellationToken))
            throw new BusinessRuleException("Eigenaar heeft nog objecten of facturen en kan niet worden verwijderd.");

        db.Owners.Remove(owner);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void Apply(Owner owner, OwnerSaveModel model)
    {
        owner.Name = model.Name.Trim();
        owner.Email = model.Email;
        owner.Phone = model.Phone;
        owner.Address = model.Address;
        owner.PostalCode = model.PostalCode;
        owner.City = model.City;
    }

    private static OwnerModel ToModel(Owner owner) => new(
        owner.Id,
        owner.Name,
        owner.Email,
        owner.Phone,
        owner.Address,
        owner.PostalCode,
        owner.City);
}
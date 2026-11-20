using Microsoft.EntityFrameworkCore;
using PropertyAdmin.Data;
using PropertyAdmin.Logic.V1.Owners.Models;

namespace PropertyAdmin.Logic.V1.Owners;

public sealed class OwnerService(AppDbContext db, TimeProvider time) : IOwnerService
{
    public async Task<IReadOnlyList<OwnerModel>> GetAllAsync(int? ownerId, CancellationToken cancellationToken)
    {
        return await db.Owners.AsNoTracking()
            .Where(owner => owner.InactiveSince == null)
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

    /// <summary>Marks the owner inactive; an owner is never deleted (Q-026, BUG-3). A repeat keeps the first date.</summary>
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var owner = await db.Owners.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (owner is null) return false;
        if (owner.InactiveSince is not null) return true;

        if (await db.Assets.AnyAsync(asset => asset.OwnerId == id, cancellationToken))
            throw new BusinessRuleException("Eigenaar heeft nog objecten en kan niet worden verwijderd.");

        owner.InactiveSince = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
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
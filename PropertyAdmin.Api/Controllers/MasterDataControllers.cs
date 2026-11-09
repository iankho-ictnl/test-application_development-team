using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyAdmin.Api.Auth;
using PropertyAdmin.Api.Services;
using PropertyAdmin.Contracts;
using PropertyAdmin.Data;

namespace PropertyAdmin.Api.Controllers;

[ApiController]
[Route("api/owners")]
public class OwnersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.Read)]
    public async Task<IReadOnlyList<OwnerDto>> List(CancellationToken ct)
    {
        var scope = User.ReadScopeOwnerId();
        var owners = await db.Owners.AsNoTracking().Where(o => scope == null || o.Id == scope).OrderBy(o => o.Name).ToListAsync(ct);
        return owners.Select(o => o.ToDto()).ToList();
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = Policies.Read)]
    public async Task<ActionResult<OwnerDto>> Get(int id, CancellationToken ct)
    {
        var scope = User.ReadScopeOwnerId();
        var o = await db.Owners.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && (scope == null || x.Id == scope));
        return o is null ? NotFound() : o.ToDto();
    }

    [HttpPost]
    [Authorize(Policy = Permissions.OwnersAssets)]
    public async Task<ActionResult<OwnerDto>> Create(OwnerSaveRequest r, CancellationToken ct)
    {
        var o = new Owner();
        Apply(o, r);
        db.Owners.Add(o);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = o.Id }, o.ToDto());
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.OwnersAssets)]
    public async Task<ActionResult<OwnerDto>> Update(int id, OwnerSaveRequest r, CancellationToken ct)
    {
        var o = await db.Owners.FindAsync(id);
        if (o is null) return NotFound();
        Apply(o, r);
        await db.SaveChangesAsync(ct);
        return o.ToDto();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.OwnersAssets)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var o = await db.Owners.FindAsync(id);
        if (o is null) return NotFound();
        if (await db.Assets.AnyAsync(a => a.OwnerId == id) || await db.Invoices.AnyAsync(i => i.OwnerId == id))
            throw new DomainException("Eigenaar heeft nog objecten of facturen en kan niet worden verwijderd.");
        db.Owners.Remove(o);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static void Apply(Owner o, OwnerSaveRequest r)
    {
        o.Name = r.Name.Trim();
        o.Email = r.Email;
        o.Phone = r.Phone;
        o.Address = r.Address;
        o.PostalCode = r.PostalCode;
        o.City = r.City;
    }
}

[ApiController]
[Route("api/assets")]
public class AssetsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.Read)]
    public async Task<IReadOnlyList<AssetDto>> List(int? ownerId, CancellationToken ct)
    {
        var scope = User.ReadScopeOwnerId();
        var list = await db.Assets.AsNoTracking().Include(a => a.Owner)
            .Where(a => (scope == null || a.OwnerId == scope) && (ownerId == null || a.OwnerId == ownerId))
            .OrderBy(a => a.Name).ToListAsync(ct);
        return list.Select(a => a.ToDto()).ToList();
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = Policies.Read)]
    public async Task<ActionResult<AssetDto>> Get(int id, CancellationToken ct)
    {
        var scope = User.ReadScopeOwnerId();
        var a = await db.Assets.AsNoTracking().Include(x => x.Owner).FirstOrDefaultAsync(x => x.Id == id && (scope == null || x.OwnerId == scope));
        return a is null ? NotFound() : a.ToDto();
    }

    [HttpPost]
    [Authorize(Policy = Permissions.OwnersAssets)]
    public async Task<ActionResult<AssetDto>> Create(AssetSaveRequest r, CancellationToken ct)
    {
        await EnsureOwner(r.OwnerId, ct);
        var a = new Asset();
        Apply(a, r);
        db.Assets.Add(a);
        await db.SaveChangesAsync(ct);
        await db.Entry(a).Reference(x => x.Owner).LoadAsync();
        return CreatedAtAction(nameof(Get), new { id = a.Id }, a.ToDto());
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.OwnersAssets)]
    public async Task<ActionResult<AssetDto>> Update(int id, AssetSaveRequest r, CancellationToken ct)
    {
        var a = await db.Assets.FindAsync(id);
        if (a is null) return NotFound();
        await EnsureOwner(r.OwnerId, ct);
        if (a.OwnerId != r.OwnerId && (await db.Activities.AnyAsync(x => x.AssetId == id && x.InvoiceId == null)
                                       || await db.Expenses.AnyAsync(x => x.AssetId == id && x.InvoiceId == null && x.Reinvoiceable)))
            throw new DomainException("Object heeft nog niet-gefactureerde regels; factureer die eerst voordat de eigenaar wijzigt.");
        Apply(a, r);
        await db.SaveChangesAsync(ct);
        await db.Entry(a).Reference(x => x.Owner).LoadAsync();
        return a.ToDto();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.OwnersAssets)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var a = await db.Assets.FindAsync(id);
        if (a is null) return NotFound();
        if (await db.Activities.AnyAsync(x => x.AssetId == id) || await db.Expenses.AnyAsync(x => x.AssetId == id))
            throw new DomainException("Object heeft nog kosten of activiteiten en kan niet worden verwijderd.");
        db.Assets.Remove(a);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task EnsureOwner(int ownerId, CancellationToken ct)
    {
        if (!await db.Owners.AnyAsync(o => o.Id == ownerId)) throw new DomainException("Eigenaar bestaat niet.");
    }

    private static void Apply(Asset a, AssetSaveRequest r)
    {
        a.OwnerId = r.OwnerId;
        a.Type = r.Type;
        a.Name = r.Name.Trim();
        a.Address = r.Address;
        a.PostalCode = r.PostalCode;
        a.City = r.City;
    }
}

[ApiController]
[Route("api/vatrates")]
public class VatRatesController(AppDbContext db, VatService vat) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<VatRateDto>> List(CancellationToken ct)
    {
        var list = await db.VatRates.AsNoTracking().OrderBy(v => v.Name).ThenByDescending(v => v.ValidFrom).ToListAsync(ct);
        return list.Select(v => v.ToDto()).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VatRateDto>> Get(int id, CancellationToken ct)
    {
        var v = await db.VatRates.FindAsync(id);
        return v is null ? NotFound() : v.ToDto();
    }

    [HttpPost]
    [Authorize(Policy = Permissions.VatRates)]
    public async Task<ActionResult<VatRateDto>> Create(VatRateSaveRequest r, CancellationToken ct)
    {
        await vat.ValidateAsync(r, null, ct);
        var v = new VatRate { Name = r.Name.Trim(), Percentage = r.Percentage, ValidFrom = r.ValidFrom, ValidTo = r.ValidTo };
        db.VatRates.Add(v);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = v.Id }, v.ToDto());
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.VatRates)]
    public async Task<ActionResult<VatRateDto>> Update(int id, VatRateSaveRequest r, CancellationToken ct)
    {
        var v = await db.VatRates.FindAsync(id);
        if (v is null) return NotFound();
        await vat.ValidateAsync(r, id, ct);
        v.Name = r.Name.Trim();
        v.Percentage = r.Percentage;
        v.ValidFrom = r.ValidFrom;
        v.ValidTo = r.ValidTo;
        await db.SaveChangesAsync(ct);
        return v.ToDto();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.VatRates)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var v = await db.VatRates.FindAsync(id);
        if (v is null) return NotFound();
        db.VatRates.Remove(v);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

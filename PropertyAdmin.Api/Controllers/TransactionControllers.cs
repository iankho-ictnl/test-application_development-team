using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyAdmin.Api.Auth;
using PropertyAdmin.Api.Services;
using PropertyAdmin.Contracts;
using PropertyAdmin.Data;

namespace PropertyAdmin.Api.Controllers;

[ApiController]
[Route("api/expenses")]
public class ExpensesController(AppDbContext db, VatService vat) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.Read)]
    public async Task<IReadOnlyList<ExpenseDto>> List(int? assetId, DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var scope = User.ReadScopeOwnerId();
        var list = await db.Expenses.AsNoTracking().Include(e => e.Asset)
            .Where(e => (scope == null || e.Asset.OwnerId == scope)
                        && (assetId == null || e.AssetId == assetId)
                        && (from == null || e.Date >= from) && (to == null || e.Date <= to))
            .OrderByDescending(e => e.Date).ThenByDescending(e => e.Id).ToListAsync(ct);
        return list.Select(e => e.ToDto()).ToList();
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = Policies.Read)]
    public async Task<ActionResult<ExpenseDto>> Get(int id, CancellationToken ct)
    {
        var scope = User.ReadScopeOwnerId();
        var e = await db.Expenses.AsNoTracking().Include(x => x.Asset).FirstOrDefaultAsync(x => x.Id == id && (scope == null || x.Asset.OwnerId == scope));
        return e is null ? NotFound() : e.ToDto();
    }

    [HttpPost]
    [Authorize(Policy = Permissions.ExpensesActivities)]
    public async Task<ActionResult<ExpenseDto>> Create(ExpenseSaveRequest r, CancellationToken ct)
    {
        var e = new Expense();
        await Apply(e, r, ct);
        db.Expenses.Add(e);
        await db.SaveChangesAsync(ct);
        await db.Entry(e).Reference(x => x.Asset).LoadAsync();
        return CreatedAtAction(nameof(Get), new { id = e.Id }, e.ToDto());
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.ExpensesActivities)]
    public async Task<ActionResult<ExpenseDto>> Update(int id, ExpenseSaveRequest r, CancellationToken ct)
    {
        var e = await db.Expenses.FindAsync(id);
        if (e is null) return NotFound();
        if (e.InvoiceId is not null) throw new DomainException("Deze kosten zijn gefactureerd en kunnen niet worden gewijzigd.");
        await Apply(e, r, ct);
        await db.SaveChangesAsync(ct);
        await db.Entry(e).Reference(x => x.Asset).LoadAsync();
        return e.ToDto();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.ExpensesActivities)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var e = await db.Expenses.FindAsync(id);
        if (e is null) return NotFound();
        if (e.InvoiceId is not null) throw new DomainException("Deze kosten zijn gefactureerd en kunnen niet worden verwijderd.");
        db.Expenses.Remove(e);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task Apply(Expense e, ExpenseSaveRequest r, CancellationToken ct)
    {
        if (!await db.Assets.AnyAsync(a => a.Id == r.AssetId)) throw new DomainException("Object bestaat niet.");
        e.AssetId = r.AssetId;
        e.Date = r.Date;
        e.Description = r.Description.Trim();
        e.Category = r.Category;
        e.AmountExclVat = Money.Round(r.AmountExclVat);
        e.VatRateName = r.VatRateName;
        e.VatPercentage = await vat.GetPercentageAsync(r.VatRateName, r.Date, ct);
        e.Reinvoiceable = r.Reinvoiceable;
    }
}

[ApiController]
[Route("api/activities")]
public class ActivitiesController(AppDbContext db, VatService vat) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.Read)]
    public async Task<IReadOnlyList<ActivityDto>> List(int? assetId, DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var scope = User.ReadScopeOwnerId();
        var list = await db.Activities.AsNoTracking().Include(a => a.Asset)
            .Where(a => (scope == null || a.Asset.OwnerId == scope)
                        && (assetId == null || a.AssetId == assetId)
                        && (from == null || a.Date >= from) && (to == null || a.Date <= to))
            .OrderByDescending(a => a.Date).ThenByDescending(a => a.Id).ToListAsync(ct);
        return list.Select(a => a.ToDto()).ToList();
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = Policies.Read)]
    public async Task<ActionResult<ActivityDto>> Get(int id, CancellationToken ct)
    {
        var scope = User.ReadScopeOwnerId();
        var a = await db.Activities.AsNoTracking().Include(x => x.Asset).FirstOrDefaultAsync(x => x.Id == id && (scope == null || x.Asset.OwnerId == scope));
        return a is null ? NotFound() : a.ToDto();
    }

    [HttpPost]
    [Authorize(Policy = Permissions.ExpensesActivities)]
    public async Task<ActionResult<ActivityDto>> Create(ActivitySaveRequest r, CancellationToken ct)
    {
        var a = new Activity();
        await Apply(a, r, ct);
        db.Activities.Add(a);
        await db.SaveChangesAsync(ct);
        await db.Entry(a).Reference(x => x.Asset).LoadAsync();
        return CreatedAtAction(nameof(Get), new { id = a.Id }, a.ToDto());
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.ExpensesActivities)]
    public async Task<ActionResult<ActivityDto>> Update(int id, ActivitySaveRequest r, CancellationToken ct)
    {
        var a = await db.Activities.FindAsync(id);
        if (a is null) return NotFound();
        if (a.InvoiceId is not null) throw new DomainException("Deze activiteit is gefactureerd en kan niet worden gewijzigd.");
        await Apply(a, r, ct);
        await db.SaveChangesAsync(ct);
        await db.Entry(a).Reference(x => x.Asset).LoadAsync();
        return a.ToDto();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.ExpensesActivities)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var a = await db.Activities.FindAsync(id);
        if (a is null) return NotFound();
        if (a.InvoiceId is not null) throw new DomainException("Deze activiteit is gefactureerd en kan niet worden verwijderd.");
        db.Activities.Remove(a);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task Apply(Activity a, ActivitySaveRequest r, CancellationToken ct)
    {
        if (!await db.Assets.AnyAsync(x => x.Id == r.AssetId)) throw new DomainException("Object bestaat niet.");
        a.AmountExclVat = InvoiceService.ActivityAmount(r.PricingType, r.Hours, r.HourlyRate, r.FixedPrice);
        a.AssetId = r.AssetId;
        a.Date = r.Date;
        a.Kind = r.Kind;
        a.Description = r.Description.Trim();
        a.PricingType = r.PricingType;
        a.Hours = r.PricingType == PricingType.Hourly ? r.Hours : null;
        a.HourlyRate = r.PricingType == PricingType.Hourly ? r.HourlyRate : null;
        a.FixedPrice = r.PricingType == PricingType.Fixed ? r.FixedPrice : null;
        a.VatRateName = r.VatRateName;
        a.VatPercentage = await vat.GetPercentageAsync(r.VatRateName, r.Date, ct);
    }
}

[ApiController]
[Route("api/invoices")]
public class InvoicesController(AppDbContext db, InvoiceService invoices, InvoicePdfService pdf) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.Read)]
    public async Task<IReadOnlyList<InvoiceDto>> List(int? ownerId, InvoiceStatus? status, CancellationToken ct)
    {
        var scope = User.ReadScopeOwnerId();
        var list = await db.Invoices.AsNoTracking().Include(i => i.Owner).Include(i => i.Lines)
            .Where(i => (scope == null || i.OwnerId == scope) && (ownerId == null || i.OwnerId == ownerId) && (status == null || i.Status == status))
            .OrderByDescending(i => i.Date).ThenByDescending(i => i.Id).ToListAsync(ct);
        return list.Select(i => i.ToDto()).ToList();
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = Policies.Read)]
    public async Task<ActionResult<InvoiceDto>> Get(int id, CancellationToken ct)
    {
        var i = await FindScoped(id, ct);
        return i is null ? NotFound() : i.ToDto();
    }

    [HttpGet("{id:int}/pdf")]
    [Authorize(Policy = Policies.Read)]
    public async Task<IActionResult> Pdf(int id, CancellationToken ct)
    {
        var i = await FindScoped(id, ct);
        if (i is null) return NotFound();
        var bytes = pdf.Render(i.ToDto(), i.Owner.ToDto());
        return File(bytes, "application/pdf", $"Factuur-{i.Number}.pdf");
    }

    [HttpGet("uninvoiced/{ownerId:int}")]
    [Authorize(Policy = Permissions.Invoices)]
    public Task<UninvoicedDto> Uninvoiced(int ownerId, CancellationToken ct) => invoices.GetUninvoicedAsync(ownerId, ct);

    [HttpPost]
    [Authorize(Policy = Permissions.Invoices)]
    public async Task<ActionResult<InvoiceDto>> Create(InvoiceCreateRequest r, CancellationToken ct)
    {
        var invoice = await invoices.CreateAsync(r, ct);
        var dto = (await FindScoped(invoice.Id, ct))!.ToDto();
        return CreatedAtAction(nameof(Get), new { id = invoice.Id }, dto);
    }

    [HttpPost("{id:int}/pay")]
    [Authorize(Policy = Permissions.Invoices)]
    public async Task<IActionResult> Pay(int id, MarkPaidRequest r, CancellationToken ct)
    {
        await invoices.MarkPaidAsync(id, r.PaidDate, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.Invoices)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await invoices.DeleteAsync(id, ct);
        return NoContent();
    }

    private Task<Invoice?> FindScoped(int id, CancellationToken ct)
    {
        var scope = User.ReadScopeOwnerId();
        return db.Invoices.AsNoTracking().Include(i => i.Owner).Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id && (scope == null || i.OwnerId == scope));
    }
}

[ApiController]
[Route("api/reports")]
[Authorize(Policy = Permissions.Reports)]
public class ReportsController(ReportService reports) : ControllerBase
{
    [HttpGet("asset-results")]
    public Task<IReadOnlyList<AssetResultDto>> AssetResults(DateOnly from, DateOnly to, CancellationToken ct) =>
        reports.AssetResultsAsync(from, to, User.ReadScopeOwnerId(), ct);

    [HttpGet("owner-overview")]
    public Task<IReadOnlyList<OwnerOverviewDto>> OwnerOverview(DateOnly from, DateOnly to, CancellationToken ct) =>
        reports.OwnerOverviewAsync(from, to, User.ReadScopeOwnerId(), ct);
}

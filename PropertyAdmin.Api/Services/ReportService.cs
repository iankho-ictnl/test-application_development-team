using Microsoft.EntityFrameworkCore;
using PropertyAdmin.Contracts;
using PropertyAdmin.Data;

namespace PropertyAdmin.Api.Services;

public class ReportService(AppDbContext db)
{
    public async Task<IReadOnlyList<AssetResultDto>> AssetResultsAsync(DateOnly from, DateOnly to, int? ownerScope, CancellationToken ct = default)
    {
        var assets = await db.Assets.AsNoTracking().Include(a => a.Owner)
            .Where(a => ownerScope == null || a.OwnerId == ownerScope)
            .OrderBy(a => a.Owner.Name).ThenBy(a => a.Name).ToListAsync(ct);
        var income = await db.Activities.AsNoTracking()
            .Where(a => a.Date >= from && a.Date <= to && (ownerScope == null || a.Asset.OwnerId == ownerScope))
            .Select(a => new { a.AssetId, a.AmountExclVat }).ToListAsync(ct);
        var expenses = await db.Expenses.AsNoTracking()
            .Where(e => e.Date >= from && e.Date <= to && (ownerScope == null || e.Asset.OwnerId == ownerScope))
            .Select(e => new { e.AssetId, e.AmountExclVat }).ToListAsync(ct);

        var inc = income.GroupBy(x => x.AssetId).ToDictionary(g => g.Key, g => g.Sum(x => x.AmountExclVat));
        var exp = expenses.GroupBy(x => x.AssetId).ToDictionary(g => g.Key, g => g.Sum(x => x.AmountExclVat));

        return assets.Select(a =>
        {
            var i = inc.GetValueOrDefault(a.Id);
            var e = exp.GetValueOrDefault(a.Id);
            return new AssetResultDto(a.Id, a.Name, a.Owner.Name, i, e, i - e);
        }).ToList();
    }

    public async Task<IReadOnlyList<OwnerOverviewDto>> OwnerOverviewAsync(DateOnly from, DateOnly to, int? ownerScope, CancellationToken ct = default)
    {
        var owners = await db.Owners.AsNoTracking()
            .Where(o => ownerScope == null || o.Id == ownerScope)
            .Select(o => new { o.Id, o.Name, AssetCount = o.Assets.Count })
            .OrderBy(o => o.Name).ToListAsync(ct);
        var assetResults = await AssetResultsAsync(from, to, ownerScope, ct);
        var assetOwner = await db.Assets.AsNoTracking().Select(a => new { a.Id, a.OwnerId }).ToDictionaryAsync(a => a.Id, a => a.OwnerId);
        var openInvoices = await db.Invoices.AsNoTracking().Include(i => i.Lines)
            .Where(i => i.Status == InvoiceStatus.Open && (ownerScope == null || i.OwnerId == ownerScope))
            .ToListAsync(ct);

        return owners.Select(o =>
        {
            var results = assetResults.Where(r => assetOwner.GetValueOrDefault(r.AssetId) == o.Id).ToList();
            var open = openInvoices.Where(i => i.OwnerId == o.Id).Select(i => i.ToDto()).ToList();
            return new OwnerOverviewDto(o.Id, o.Name, o.AssetCount, results.Sum(r => r.Income), results.Sum(r => r.Expenses),
                open.Count, open.Sum(i => i.TotalInclVat));
        }).ToList();
    }
}

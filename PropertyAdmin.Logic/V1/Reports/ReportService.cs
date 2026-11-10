using Microsoft.EntityFrameworkCore;
using PropertyAdmin.Data;
using PropertyAdmin.Logic.V1.Reports.Models;

namespace PropertyAdmin.Logic.V1.Reports;

public sealed class ReportService(AppDbContext db) : IReportService
{
    public async Task<IReadOnlyList<AssetResultModel>> GetAssetResultsAsync(DateOnly from, DateOnly to, int? ownerScopeId, CancellationToken cancellationToken)
    {
        var assets = await db.Assets.AsNoTracking()
            .Where(asset => ownerScopeId == null || asset.OwnerId == ownerScopeId)
            .OrderBy(asset => asset.Owner.Name).ThenBy(asset => asset.Name)
            .Select(asset => new { asset.Id, asset.Name, asset.OwnerId, OwnerName = asset.Owner.Name })
            .ToListAsync(cancellationToken);
        var income = await db.Activities.AsNoTracking()
            .Where(activity => activity.Date >= from && activity.Date <= to && (ownerScopeId == null || activity.Asset.OwnerId == ownerScopeId))
            .Select(activity => new { activity.AssetId, activity.AmountExclVat })
            .ToListAsync(cancellationToken);
        var expenses = await db.Expenses.AsNoTracking()
            .Where(expense => expense.Date >= from && expense.Date <= to && (ownerScopeId == null || expense.Asset.OwnerId == ownerScopeId))
            .Select(expense => new { expense.AssetId, expense.AmountExclVat })
            .ToListAsync(cancellationToken);

        var incomePerAsset = income.GroupBy(item => item.AssetId).ToDictionary(group => group.Key, group => group.Sum(item => item.AmountExclVat));
        var expensesPerAsset = expenses.GroupBy(item => item.AssetId).ToDictionary(group => group.Key, group => group.Sum(item => item.AmountExclVat));

        return assets.Select(asset => new AssetResultModel(asset.Id, asset.Name, asset.OwnerId, asset.OwnerName,
            incomePerAsset.GetValueOrDefault(asset.Id), expensesPerAsset.GetValueOrDefault(asset.Id))).ToList();
    }

    public async Task<IReadOnlyList<OwnerOverviewModel>> GetOwnerOverviewAsync(DateOnly from, DateOnly to, int? ownerScopeId, CancellationToken cancellationToken)
    {
        var owners = await db.Owners.AsNoTracking()
            .Where(owner => ownerScopeId == null || owner.Id == ownerScopeId)
            .OrderBy(owner => owner.Name)
            .Select(owner => new { owner.Id, owner.Name, AssetCount = owner.Assets.Count })
            .ToListAsync(cancellationToken);
        var assetResults = await GetAssetResultsAsync(from, to, ownerScopeId, cancellationToken);
        var openInvoices = await db.Invoices.AsNoTracking()
            .Where(invoice => invoice.Status == InvoiceStatus.Open && (ownerScopeId == null || invoice.OwnerId == ownerScopeId))
            .Select(invoice => new { invoice.OwnerId, Lines = invoice.Lines.Select(line => new { line.AmountExclVat, line.VatPercentage }).ToList() })
            .ToListAsync(cancellationToken);

        return owners.Select(owner =>
        {
            var results = assetResults.Where(result => result.OwnerId == owner.Id).ToList();
            var open = openInvoices.Where(invoice => invoice.OwnerId == owner.Id).ToList();
            return new OwnerOverviewModel(owner.Id, owner.Name, owner.AssetCount, results.Sum(result => result.Income),
                results.Sum(result => result.Expenses), open.Count,
                open.Sum(invoice => InvoiceTotals.TotalInclVat(invoice.Lines.Select(line => (line.AmountExclVat, line.VatPercentage)))));
        }).ToList();
    }
}

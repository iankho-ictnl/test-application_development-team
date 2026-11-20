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
        // Q-112 b, Q-113: an invoiced line counts under the invoice's owner, an uninvoiced line under the
        // asset's current owner. The owner scope filters on that same per-line owner (T-143).
        var income = await db.Activities.AsNoTracking()
            .Where(activity => activity.Date >= from && activity.Date <= to)
            .Select(activity => new { OwnerId = activity.InvoiceId != null ? activity.Invoice!.OwnerId : activity.Asset.OwnerId, activity.AmountExclVat })
            .Where(line => ownerScopeId == null || line.OwnerId == ownerScopeId)
            .ToListAsync(cancellationToken);
        var expenses = await db.Expenses.AsNoTracking()
            .Where(expense => expense.Date >= from && expense.Date <= to)
            .Select(expense => new { OwnerId = expense.InvoiceId != null ? expense.Invoice!.OwnerId : expense.Asset.OwnerId, expense.AmountExclVat })
            .Where(line => ownerScopeId == null || line.OwnerId == ownerScopeId)
            .ToListAsync(cancellationToken);
        var incomePerOwner = income.GroupBy(line => line.OwnerId).ToDictionary(group => group.Key, group => group.Sum(line => line.AmountExclVat));
        var expensesPerOwner = expenses.GroupBy(line => line.OwnerId).ToDictionary(group => group.Key, group => group.Sum(line => line.AmountExclVat));
        var openInvoices = await db.Invoices.AsNoTracking()
            .Where(invoice => invoice.Status == InvoiceStatus.Open && (ownerScopeId == null || invoice.OwnerId == ownerScopeId))
            .Select(invoice => new { invoice.OwnerId, Lines = invoice.Lines.Select(line => new { line.AmountExclVat, line.VatPercentage }).ToList() })
            .ToListAsync(cancellationToken);

        return owners.Select(owner =>
        {
            var open = openInvoices.Where(invoice => invoice.OwnerId == owner.Id).ToList();
            return new OwnerOverviewModel(owner.Id, owner.Name, owner.AssetCount, incomePerOwner.GetValueOrDefault(owner.Id),
                expensesPerOwner.GetValueOrDefault(owner.Id), open.Count,
                open.Sum(invoice => InvoiceTotals.TotalInclVat(invoice.Lines.Select(line => (line.AmountExclVat, line.VatPercentage)))));
        }).ToList();
    }
}

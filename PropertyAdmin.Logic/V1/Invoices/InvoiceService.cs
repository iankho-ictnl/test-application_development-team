using Microsoft.EntityFrameworkCore;
using PropertyAdmin.Contracts;
using PropertyAdmin.Data;
using PropertyAdmin.Logic.V1.Activities.Models;
using PropertyAdmin.Logic.V1.Expenses.Models;
using PropertyAdmin.Logic.V1.Invoices.Models;

namespace PropertyAdmin.Logic.V1.Invoices;

public sealed class InvoiceService(AppDbContext db) : IInvoiceService
{
    private const int MaxNumberAttempts = 3;

    public async Task<IReadOnlyList<InvoiceModel>> GetAllAsync(int? ownerScopeId, int? ownerId, InvoiceStatus? status, CancellationToken cancellationToken) =>
        await db.Invoices.AsNoTracking()
            .Where(invoice => (ownerScopeId == null || invoice.OwnerId == ownerScopeId)
                && (ownerId == null || invoice.OwnerId == ownerId)
                && (status == null || invoice.Status == status))
            .OrderByDescending(invoice => invoice.Date).ThenByDescending(invoice => invoice.Id)
            .Select(invoice => new InvoiceModel(invoice.Id, invoice.Number, invoice.OwnerId, invoice.Owner.Name, invoice.Date,
                invoice.DueDate, invoice.Status, invoice.PaidDate,
                invoice.Lines.OrderBy(line => line.Id).Select(line => new InvoiceLineModel(line.Description, line.AmountExclVat, line.VatPercentage)).ToList()))
            .ToListAsync(cancellationToken);

    public async Task<InvoiceModel?> GetByIdAsync(int id, int? ownerScopeId, CancellationToken cancellationToken) =>
        await db.Invoices.AsNoTracking()
            .Where(invoice => invoice.Id == id && (ownerScopeId == null || invoice.OwnerId == ownerScopeId))
            .Select(invoice => new InvoiceModel(invoice.Id, invoice.Number, invoice.OwnerId, invoice.Owner.Name, invoice.Date,
                invoice.DueDate, invoice.Status, invoice.PaidDate,
                invoice.Lines.OrderBy(line => line.Id).Select(line => new InvoiceLineModel(line.Description, line.AmountExclVat, line.VatPercentage)).ToList()))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<UninvoicedModel> GetUninvoicedAsync(int ownerId, CancellationToken cancellationToken)
    {
        var activities = await db.Activities.AsNoTracking()
            .Where(activity => activity.InvoiceId == null && activity.Asset.OwnerId == ownerId)
            .OrderBy(activity => activity.Date)
            .Select(activity => new ActivityModel(activity.Id, activity.AssetId, activity.Asset.Name, activity.Date, activity.Kind,
                activity.Description, activity.PricingType, activity.Hours, activity.HourlyRate, activity.FixedPrice,
                activity.AmountExclVat, activity.VatRateName, activity.VatPercentage, activity.InvoiceId))
            .ToListAsync(cancellationToken);
        var expenses = await db.Expenses.AsNoTracking()
            .Where(expense => expense.InvoiceId == null && expense.Reinvoiceable && expense.Asset.OwnerId == ownerId)
            .OrderBy(expense => expense.Date)
            .Select(expense => new ExpenseModel(expense.Id, expense.AssetId, expense.Asset.Name, expense.Date, expense.Description,
                expense.Category, expense.AmountExclVat, expense.VatRateName, expense.VatPercentage, expense.Reinvoiceable, expense.InvoiceId))
            .ToListAsync(cancellationToken);
        return new UninvoicedModel(activities, expenses);
    }

    public async Task<InvoiceModel> CreateAsync(InvoiceCreateModel model, CancellationToken cancellationToken)
    {
        if (!await db.Owners.AnyAsync(owner => owner.Id == model.OwnerId, cancellationToken))
            throw new BusinessRuleException("Eigenaar bestaat niet.");

        for (var attempt = 1; ; attempt++)
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var invoice = await BuildInvoiceAsync(model, cancellationToken);
                db.Invoices.Add(invoice);
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return (await GetByIdAsync(invoice.Id, null, cancellationToken))!;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (DbUpdateException) when (attempt < MaxNumberAttempts)
            {
                // A parallel invoice took the same number: retry with the next one (D-008).
                await transaction.RollbackAsync(cancellationToken);
                db.ChangeTracker.Clear();
            }
        }
    }

    public async Task<bool> MarkPaidAsync(int id, DateOnly paidDate, CancellationToken cancellationToken)
    {
        var invoice = await db.Invoices.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (invoice is null) return false;
        if (invoice.Status == InvoiceStatus.Paid) throw new BusinessRuleException("Factuur is al betaald.");
        if (paidDate < invoice.Date) throw new BusinessRuleException("Betaaldatum ligt voor de factuurdatum.");

        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidDate = paidDate;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var invoice = await db.Invoices.Include(item => item.Lines).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (invoice is null) return false;
        if (invoice.Status == InvoiceStatus.Paid) throw new BusinessRuleException("Een betaalde factuur kan niet worden verwijderd.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        foreach (var activity in await db.Activities.Where(activity => activity.InvoiceId == id).ToListAsync(cancellationToken))
            activity.InvoiceId = null;
        foreach (var expense in await db.Expenses.Where(expense => expense.InvoiceId == id).ToListAsync(cancellationToken))
            expense.InvoiceId = null;
        db.Invoices.Remove(invoice);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<Invoice> BuildInvoiceAsync(InvoiceCreateModel model, CancellationToken cancellationToken)
    {
        var activities = await db.Activities.Include(activity => activity.Asset)
            .Where(activity => activity.InvoiceId == null && activity.Asset.OwnerId == model.OwnerId && activity.Date <= model.Date)
            .OrderBy(activity => activity.Date).ToListAsync(cancellationToken);
        var expenses = await db.Expenses.Include(expense => expense.Asset)
            .Where(expense => expense.InvoiceId == null && expense.Reinvoiceable && expense.Asset.OwnerId == model.OwnerId && expense.Date <= model.Date)
            .OrderBy(expense => expense.Date).ToListAsync(cancellationToken);
        if (activities.Count == 0 && expenses.Count == 0)
            throw new BusinessRuleException("Er zijn geen te factureren activiteiten of kosten voor deze eigenaar.");

        var invoice = new Invoice
        {
            Number = await NextNumberAsync(model.Date.Year, cancellationToken),
            OwnerId = model.OwnerId,
            Date = model.Date,
            DueDate = model.Date.AddDays(model.PaymentTermDays),
            Status = InvoiceStatus.Open,
        };
        foreach (var activity in activities)
        {
            invoice.Lines.Add(Line(activity.Date, activity.Asset.Name, activity.Description, activity.AmountExclVat,
                activity.VatPercentage, InvoiceLineSource.Activity, activity.Id));
            activity.Invoice = invoice;
        }
        foreach (var expense in expenses)
        {
            invoice.Lines.Add(Line(expense.Date, expense.Asset.Name, expense.Description, expense.AmountExclVat,
                expense.VatPercentage, InvoiceLineSource.Expense, expense.Id));
            expense.Invoice = invoice;
        }
        return invoice;
    }

    private static InvoiceLine Line(DateOnly date, string assetName, string description, decimal amountExclVat, decimal vatPercentage,
        InvoiceLineSource sourceType, int sourceId) => new()
    {
        Description = $"{date:dd-MM-yyyy} {assetName}: {description}",
        AmountExclVat = amountExclVat,
        VatPercentage = vatPercentage,
        SourceType = sourceType,
        SourceId = sourceId,
    };

    private async Task<string> NextNumberAsync(int year, CancellationToken cancellationToken)
    {
        var prefix = $"{year}-";
        var numbers = await db.Invoices.AsNoTracking()
            .Where(invoice => invoice.Number.StartsWith(prefix))
            .Select(invoice => invoice.Number)
            .ToListAsync(cancellationToken);
        var max = numbers.Select(number => int.TryParse(number[prefix.Length..], out var value) ? value : 0).DefaultIfEmpty(0).Max();
        return $"{prefix}{max + 1:0000}";
    }
}

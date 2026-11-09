using Microsoft.EntityFrameworkCore;
using PropertyAdmin.Contracts;
using PropertyAdmin.Data;

namespace PropertyAdmin.Api.Services;

public class InvoiceService(AppDbContext db)
{
    public static decimal ActivityAmount(PricingType type, decimal? hours, decimal? rate, decimal? fixedPrice) => type switch
    {
        PricingType.Hourly when hours is > 0 && rate is >= 0 => Money.Round(hours.Value * rate.Value),
        PricingType.Fixed when fixedPrice is >= 0 => Money.Round(fixedPrice.Value),
        PricingType.Hourly => throw new DomainException("Vul uren en uurtarief in."),
        _ => throw new DomainException("Vul een vaste prijs in."),
    };

    public async Task<UninvoicedDto> GetUninvoicedAsync(int ownerId, CancellationToken ct = default)
    {
        var activities = await db.Activities.AsNoTracking().Include(a => a.Asset)
            .Where(a => a.InvoiceId == null && a.Asset.OwnerId == ownerId)
            .OrderBy(a => a.Date).ToListAsync(ct);
        var expenses = await db.Expenses.AsNoTracking().Include(e => e.Asset)
            .Where(e => e.InvoiceId == null && e.Reinvoiceable && e.Asset.OwnerId == ownerId)
            .OrderBy(e => e.Date).ToListAsync(ct);
        return new(activities.Select(a => a.ToDto()).ToList(), expenses.Select(e => e.ToDto()).ToList());
    }

    public async Task<Invoice> CreateAsync(InvoiceCreateRequest request, CancellationToken ct = default)
    {
        if (!await db.Owners.AnyAsync(o => o.Id == request.OwnerId))
            throw new DomainException("Eigenaar bestaat niet.");

        for (var attempt = 1; ; attempt++)
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            try
            {
                var activities = await db.Activities.Include(a => a.Asset)
                    .Where(a => a.InvoiceId == null && a.Asset.OwnerId == request.OwnerId && a.Date <= request.Date)
                    .OrderBy(a => a.Date).ToListAsync(ct);
                var expenses = await db.Expenses.Include(e => e.Asset)
                    .Where(e => e.InvoiceId == null && e.Reinvoiceable && e.Asset.OwnerId == request.OwnerId && e.Date <= request.Date)
                    .OrderBy(e => e.Date).ToListAsync(ct);
                if (activities.Count == 0 && expenses.Count == 0)
                    throw new DomainException("Er zijn geen te factureren activiteiten of kosten voor deze eigenaar.");

                var invoice = new Invoice
                {
                    Number = await NextNumberAsync(request.Date.Year, ct),
                    OwnerId = request.OwnerId,
                    Date = request.Date,
                    DueDate = request.Date.AddDays(request.PaymentTermDays),
                    Status = InvoiceStatus.Open,
                };
                foreach (var a in activities)
                {
                    invoice.Lines.Add(new InvoiceLine
                    {
                        Description = $"{a.Date:dd-MM-yyyy} {a.Asset.Name}: {a.Description}",
                        AmountExclVat = a.AmountExclVat,
                        VatPercentage = a.VatPercentage,
                        SourceType = InvoiceLineSource.Activity,
                        SourceId = a.Id,
                    });
                    a.Invoice = invoice;
                }
                foreach (var e in expenses)
                {
                    invoice.Lines.Add(new InvoiceLine
                    {
                        Description = $"{e.Date:dd-MM-yyyy} {e.Asset.Name}: {e.Description}",
                        AmountExclVat = e.AmountExclVat,
                        VatPercentage = e.VatPercentage,
                        SourceType = InvoiceLineSource.Expense,
                        SourceId = e.Id,
                    });
                    e.Invoice = invoice;
                }
                db.Invoices.Add(invoice);
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return invoice;
            }
            catch (DbUpdateException) when (attempt < 3)
            {
                await tx.RollbackAsync(ct);
                db.ChangeTracker.Clear();
            }
        }
    }

    public async Task MarkPaidAsync(int id, DateOnly paidDate, CancellationToken ct = default)
    {
        var invoice = await db.Invoices.FindAsync(id) ?? throw new KeyNotFoundException();
        if (invoice.Status == InvoiceStatus.Paid) throw new DomainException("Factuur is al betaald.");
        if (paidDate < invoice.Date) throw new DomainException("Betaaldatum ligt voor de factuurdatum.");
        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidDate = paidDate;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var invoice = await db.Invoices.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == id) ?? throw new KeyNotFoundException();
        if (invoice.Status == InvoiceStatus.Paid) throw new DomainException("Een betaalde factuur kan niet worden verwijderd.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        foreach (var a in await db.Activities.Where(a => a.InvoiceId == id).ToListAsync(ct)) a.InvoiceId = null;
        foreach (var e in await db.Expenses.Where(e => e.InvoiceId == id).ToListAsync(ct)) e.InvoiceId = null;
        db.Invoices.Remove(invoice);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private async Task<string> NextNumberAsync(int year, CancellationToken ct = default)
    {
        var prefix = $"{year}-";
        var numbers = await db.Invoices.AsNoTracking().Where(i => i.Number.StartsWith(prefix)).Select(i => i.Number).ToListAsync(ct);
        var max = numbers.Select(n => int.TryParse(n[prefix.Length..], out var x) ? x : 0).DefaultIfEmpty(0).Max();
        return $"{prefix}{max + 1:0000}";
    }
}

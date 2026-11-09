using Microsoft.EntityFrameworkCore;
using PropertyAdmin.Contracts;
using PropertyAdmin.Data;

namespace PropertyAdmin.Api.Services;

public class DomainException(string message) : Exception(message);

public static class Money
{
    public static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    public static decimal Vat(decimal amountExclVat, decimal percentage) => Round(amountExclVat * percentage / 100m);
}

public class VatService(AppDbContext db)
{
    public async Task<decimal> GetPercentageAsync(string name, DateOnly date, CancellationToken ct = default)
    {
        var rate = await db.VatRates.AsNoTracking()
            .Where(r => r.Name == name && r.ValidFrom <= date && (r.ValidTo == null || r.ValidTo >= date))
            .FirstOrDefaultAsync(ct);
        return rate?.Percentage
            ?? throw new DomainException($"Er is geen btw-tarief '{name}' geldig op {date:dd-MM-yyyy}.");
    }

    public async Task ValidateAsync(VatRateSaveRequest r, int? excludeId, CancellationToken ct = default)
    {
        if (r.ValidTo is DateOnly to && to < r.ValidFrom)
            throw new DomainException("De einddatum ligt voor de begindatum.");

        var others = await db.VatRates.AsNoTracking()
            .Where(x => x.Name == r.Name && x.Id != (excludeId ?? 0))
            .ToListAsync(ct);
        var newEnd = r.ValidTo ?? DateOnly.MaxValue;
        if (others.Any(o => o.ValidFrom <= newEnd && r.ValidFrom <= (o.ValidTo ?? DateOnly.MaxValue)))
            throw new DomainException($"De geldigheidsperiode overlapt met een ander tarief '{r.Name}'.");
    }
}

using Microsoft.EntityFrameworkCore;
using PropertyAdmin.Data;
using PropertyAdmin.Logic.V1.VatRates.Models;

namespace PropertyAdmin.Logic.V1.VatRates;

public sealed class VatRateService(AppDbContext db) : IVatRateService
{
    public async Task<IReadOnlyList<VatRateModel>> GetAllAsync(CancellationToken cancellationToken) =>
        await db.VatRates.AsNoTracking()
            .OrderBy(rate => rate.Name).ThenByDescending(rate => rate.ValidFrom)
            .Select(rate => new VatRateModel(rate.Id, rate.Name, rate.Percentage, rate.ValidFrom, rate.ValidTo))
            .ToListAsync(cancellationToken);

    public async Task<VatRateModel?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        await db.VatRates.AsNoTracking()
            .Where(rate => rate.Id == id)
            .Select(rate => new VatRateModel(rate.Id, rate.Name, rate.Percentage, rate.ValidFrom, rate.ValidTo))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<VatRateModel> CreateAsync(VatRateSaveModel model, CancellationToken cancellationToken)
    {
        await ValidateAsync(model, null, cancellationToken);
        var rate = new VatRate();
        Apply(rate, model);
        db.VatRates.Add(rate);
        await db.SaveChangesAsync(cancellationToken);
        return ToModel(rate);
    }

    public async Task<VatRateModel?> UpdateAsync(int id, VatRateSaveModel model, CancellationToken cancellationToken)
    {
        var rate = await db.VatRates.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (rate is null) return null;

        await ValidateAsync(model, id, cancellationToken);
        Apply(rate, model);
        await db.SaveChangesAsync(cancellationToken);
        return ToModel(rate);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var rate = await db.VatRates.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (rate is null) return false;
        db.VatRates.Remove(rate);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<decimal> GetPercentageAsync(string name, DateOnly date, CancellationToken cancellationToken)
    {
        var percentage = await db.VatRates.AsNoTracking()
            .Where(rate => rate.Name == name && rate.ValidFrom <= date && (rate.ValidTo == null || rate.ValidTo >= date))
            .Select(rate => (decimal?)rate.Percentage)
            .FirstOrDefaultAsync(cancellationToken);
        return percentage
            ?? throw new BusinessRuleException($"Er is geen btw-tarief '{name}' geldig op {date:dd-MM-yyyy}.");
    }

    private async Task ValidateAsync(VatRateSaveModel model, int? excludeId, CancellationToken cancellationToken)
    {
        if (model.ValidTo is DateOnly validTo && validTo < model.ValidFrom)
            throw new BusinessRuleException("De einddatum ligt voor de begindatum.");

        var rates = await db.VatRates.AsNoTracking()
            .Where(rate => rate.Name == model.Name && rate.Id != (excludeId ?? 0))
            .ToListAsync(cancellationToken);
        var newEnd = model.ValidTo ?? DateOnly.MaxValue;
        if (rates.Any(rate => rate.ValidFrom <= newEnd && model.ValidFrom <= (rate.ValidTo ?? DateOnly.MaxValue)))
            throw new BusinessRuleException($"De geldigheidsperiode overlapt met een ander tarief '{model.Name}'.");
    }

    private static void Apply(VatRate rate, VatRateSaveModel model)
    {
        rate.Name = model.Name.Trim();
        rate.Percentage = model.Percentage;
        rate.ValidFrom = model.ValidFrom;
        rate.ValidTo = model.ValidTo;
    }

    private static VatRateModel ToModel(VatRate rate) =>
        new(rate.Id, rate.Name, rate.Percentage, rate.ValidFrom, rate.ValidTo);
}
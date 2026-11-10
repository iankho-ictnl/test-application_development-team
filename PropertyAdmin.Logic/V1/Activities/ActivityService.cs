using Microsoft.EntityFrameworkCore;
using PropertyAdmin.Data;
using PropertyAdmin.Logic.V1.Activities.Models;
using PropertyAdmin.Logic.V1.VatRates;

namespace PropertyAdmin.Logic.V1.Activities;

public sealed class ActivityService(AppDbContext db, IVatRateService vatRates) : IActivityService
{
    public static decimal CalculateAmount(PricingType type, decimal? hours, decimal? rate, decimal? fixedPrice) => type switch
    {
        PricingType.Hourly when hours is > 0 && rate is >= 0 => Money.Round(hours.Value * rate.Value),
        PricingType.Fixed when fixedPrice is >= 0 => Money.Round(fixedPrice.Value),
        PricingType.Hourly => throw new BusinessRuleException("Vul uren en uurtarief in."),
        _ => throw new BusinessRuleException("Vul een vaste prijs in."),
    };

    public async Task<IReadOnlyList<ActivityModel>> GetAllAsync(int? ownerScopeId, int? assetId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken) =>
        await db.Activities.AsNoTracking()
            .Where(activity => (ownerScopeId == null || activity.Asset.OwnerId == ownerScopeId)
                && (assetId == null || activity.AssetId == assetId)
                && (from == null || activity.Date >= from)
                && (to == null || activity.Date <= to))
            .OrderByDescending(activity => activity.Date).ThenByDescending(activity => activity.Id)
            .Select(activity => new ActivityModel(activity.Id, activity.AssetId, activity.Asset.Name, activity.Date, activity.Kind,
                activity.Description, activity.PricingType, activity.Hours, activity.HourlyRate, activity.FixedPrice,
                activity.AmountExclVat, activity.VatRateName, activity.VatPercentage, activity.InvoiceId))
            .ToListAsync(cancellationToken);

    public async Task<ActivityModel?> GetByIdAsync(int id, int? ownerScopeId, CancellationToken cancellationToken) =>
        await db.Activities.AsNoTracking()
            .Where(activity => activity.Id == id && (ownerScopeId == null || activity.Asset.OwnerId == ownerScopeId))
            .Select(activity => new ActivityModel(activity.Id, activity.AssetId, activity.Asset.Name, activity.Date, activity.Kind,
                activity.Description, activity.PricingType, activity.Hours, activity.HourlyRate, activity.FixedPrice,
                activity.AmountExclVat, activity.VatRateName, activity.VatPercentage, activity.InvoiceId))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<ActivityModel> CreateAsync(ActivitySaveModel model, CancellationToken cancellationToken)
    {
        var activity = new Activity();
        var assetName = await ApplyAsync(activity, model, cancellationToken);
        db.Activities.Add(activity);
        await db.SaveChangesAsync(cancellationToken);
        return ToModel(activity, assetName);
    }

    public async Task<ActivityModel?> UpdateAsync(int id, ActivitySaveModel model, CancellationToken cancellationToken)
    {
        var activity = await db.Activities.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (activity is null) return null;
        if (activity.InvoiceId is not null)
            throw new BusinessRuleException("Deze activiteit is gefactureerd en kan niet worden gewijzigd.");

        var assetName = await ApplyAsync(activity, model, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToModel(activity, assetName);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var activity = await db.Activities.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (activity is null) return false;
        if (activity.InvoiceId is not null)
            throw new BusinessRuleException("Deze activiteit is gefactureerd en kan niet worden verwijderd.");

        db.Activities.Remove(activity);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<string> ApplyAsync(Activity activity, ActivitySaveModel model, CancellationToken cancellationToken)
    {
        var assetName = await db.Assets.AsNoTracking()
            .Where(asset => asset.Id == model.AssetId)
            .Select(asset => asset.Name)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new BusinessRuleException("Object bestaat niet.");

        var hourly = model.PricingType == PricingType.Hourly;
        activity.AmountExclVat = CalculateAmount(model.PricingType, model.Hours, model.HourlyRate, model.FixedPrice);
        activity.AssetId = model.AssetId;
        activity.Date = model.Date;
        activity.Kind = model.Kind;
        activity.Description = model.Description.Trim();
        activity.PricingType = model.PricingType;
        activity.Hours = hourly ? model.Hours : null;
        activity.HourlyRate = hourly ? model.HourlyRate : null;
        activity.FixedPrice = model.PricingType == PricingType.Fixed ? model.FixedPrice : null;
        activity.VatRateName = model.VatRateName;
        activity.VatPercentage = await vatRates.GetPercentageAsync(model.VatRateName, model.Date, cancellationToken);
        return assetName;
    }

    private static ActivityModel ToModel(Activity activity, string assetName) =>
        new(activity.Id, activity.AssetId, assetName, activity.Date, activity.Kind, activity.Description, activity.PricingType,
            activity.Hours, activity.HourlyRate, activity.FixedPrice, activity.AmountExclVat, activity.VatRateName,
            activity.VatPercentage, activity.InvoiceId);
}

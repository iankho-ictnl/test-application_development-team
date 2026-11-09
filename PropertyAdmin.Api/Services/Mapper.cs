using PropertyAdmin.Contracts;
using PropertyAdmin.Data;

namespace PropertyAdmin.Api.Services;

public static class Mapper
{
    public static OwnerDto ToDto(this Owner o) => new(o.Id, o.Name, o.Email, o.Phone, o.Address, o.PostalCode, o.City);

    public static AssetDto ToDto(this Asset a) =>
        new(a.Id, a.OwnerId, a.Owner?.Name ?? "", a.Type, a.Name, a.Address, a.PostalCode, a.City);

    public static VatRateDto ToDto(this VatRate v) => new(v.Id, v.Name, v.Percentage, v.ValidFrom, v.ValidTo);

    public static ExpenseDto ToDto(this Expense e)
    {
        var vat = Money.Vat(e.AmountExclVat, e.VatPercentage);
        return new(e.Id, e.AssetId, e.Asset?.Name ?? "", e.Date, e.Description, e.Category, e.AmountExclVat,
            e.VatRateName, e.VatPercentage, vat, e.AmountExclVat + vat, e.Reinvoiceable, e.InvoiceId);
    }

    public static ActivityDto ToDto(this Activity a)
    {
        var vat = Money.Vat(a.AmountExclVat, a.VatPercentage);
        return new(a.Id, a.AssetId, a.Asset?.Name ?? "", a.Date, a.Kind, a.Description, a.PricingType, a.Hours,
            a.HourlyRate, a.FixedPrice, a.AmountExclVat, a.VatRateName, a.VatPercentage, vat, a.AmountExclVat + vat, a.InvoiceId);
    }

    public static InvoiceDto ToDto(this Invoice i)
    {
        var lines = i.Lines.OrderBy(l => l.Id).Select(l =>
        {
            var vat = Money.Vat(l.AmountExclVat, l.VatPercentage);
            return new InvoiceLineDto(l.Description, l.AmountExclVat, l.VatPercentage, vat, l.AmountExclVat + vat);
        }).ToList();
        var summary = i.Lines.GroupBy(l => l.VatPercentage).OrderByDescending(g => g.Key).Select(g =>
        {
            var b = g.Sum(l => l.AmountExclVat);
            return new VatSummaryDto(g.Key, b, Money.Vat(b, g.Key));
        }).ToList();
        var totalExcl = summary.Sum(s => s.Base);
        var totalVat = summary.Sum(s => s.VatAmount);
        return new(i.Id, i.Number, i.OwnerId, i.Owner?.Name ?? "", i.Date, i.DueDate, i.Status, i.PaidDate,
            totalExcl, totalVat, totalExcl + totalVat, lines, summary);
    }
}

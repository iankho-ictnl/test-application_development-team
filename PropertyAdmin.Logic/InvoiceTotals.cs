namespace PropertyAdmin.Logic;

public sealed record VatSummaryLine(decimal VatPercentage, decimal Base, decimal VatAmount);

/// <summary>Invoice totals: VAT is rounded once per VAT percentage over the sum of its lines.</summary>
public static class InvoiceTotals
{
    public static IReadOnlyList<VatSummaryLine> VatSummary(IEnumerable<(decimal AmountExclVat, decimal VatPercentage)> lines) =>
        lines.GroupBy(line => line.VatPercentage).OrderByDescending(group => group.Key).Select(group =>
        {
            var vatBase = group.Sum(line => line.AmountExclVat);
            return new VatSummaryLine(group.Key, vatBase, Money.Vat(vatBase, group.Key));
        }).ToList();

    public static decimal TotalInclVat(IEnumerable<(decimal AmountExclVat, decimal VatPercentage)> lines) =>
        VatSummary(lines).Sum(summary => summary.Base + summary.VatAmount);
}

using PropertyAdmin.Logic.V1.Invoices.GetUninvoiced;

namespace PropertyAdmin.Api.V1.Invoices.GetUninvoiced;

public static class Mapping
{
    public static Query ToQuery(this Request request) => new(request.OwnerId);

    public static Response ToResponse(this Result result) => new(
        result.Uninvoiced.Activities.Select(activity => new ActivityResponse(activity.Id, activity.AssetId, activity.AssetName, activity.Date,
            (ActivityKind)activity.Kind, activity.Description, (PricingType)activity.PricingType, activity.Hours, activity.HourlyRate, activity.FixedPrice,
            activity.AmountExclVat, activity.VatRateName, activity.VatPercentage, activity.VatAmount, activity.AmountInclVat, activity.InvoiceId)).ToList(),
        result.Uninvoiced.Expenses.Select(expense => new ExpenseResponse(expense.Id, expense.AssetId, expense.AssetName, expense.Date,
            expense.Description, expense.Category, expense.AmountExclVat, expense.VatRateName, expense.VatPercentage, expense.VatAmount,
            expense.AmountInclVat, expense.Reinvoiceable, expense.InvoiceId)).ToList());
}

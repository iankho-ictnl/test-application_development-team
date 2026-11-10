using PropertyAdmin.Logic.V1.Expenses.GetById;

namespace PropertyAdmin.Api.V1.Expenses.GetById;

public static class Mapping
{
    public static Query ToQuery(this Request request, int? ownerScopeId) => new(request.Id, ownerScopeId);

    public static Response ToResponse(this Result result)
    {
        var expense = result.Expense!;
        return new Response(expense.Id, expense.AssetId, expense.AssetName, expense.Date, expense.Description, expense.Category, expense.AmountExclVat,
            expense.VatRateName, expense.VatPercentage, expense.VatAmount, expense.AmountInclVat, expense.Reinvoiceable, expense.InvoiceId);
    }
}

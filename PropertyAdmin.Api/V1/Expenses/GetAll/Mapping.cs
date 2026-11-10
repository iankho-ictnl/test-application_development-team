using PropertyAdmin.Logic.V1.Expenses.GetAll;
using PropertyAdmin.Logic.V1.Expenses.Models;

namespace PropertyAdmin.Api.V1.Expenses.GetAll;

public static class Mapping
{
    public static Query ToQuery(this Request request, int? ownerScopeId) => new(ownerScopeId, request.AssetId, request.From, request.To);

    public static IReadOnlyList<Response> ToResponse(this Result result) =>
        result.Expenses.Select(expense => expense.ToResponse()).ToList();

    private static Response ToResponse(this ExpenseModel expense) =>
        new(expense.Id, expense.AssetId, expense.AssetName, expense.Date, expense.Description, expense.Category, expense.AmountExclVat,
            expense.VatRateName, expense.VatPercentage, expense.VatAmount, expense.AmountInclVat, expense.Reinvoiceable, expense.InvoiceId);
}

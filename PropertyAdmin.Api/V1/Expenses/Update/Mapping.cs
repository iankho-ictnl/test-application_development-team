using PropertyAdmin.Logic.V1.Expenses.Models;
using PropertyAdmin.Logic.V1.Expenses.Update;

namespace PropertyAdmin.Api.V1.Expenses.Update;

public static class Mapping
{
    public static Command ToCommand(this Request request, int id) => new(id, new ExpenseSaveModel(
        request.AssetId, request.Date, request.Description, request.Category, request.AmountExclVat, request.VatRateName, request.Reinvoiceable));

    public static Response ToResponse(this Result result)
    {
        var expense = result.Expense!;
        return new Response(expense.Id, expense.AssetId, expense.AssetName, expense.Date, expense.Description, expense.Category, expense.AmountExclVat,
            expense.VatRateName, expense.VatPercentage, expense.VatAmount, expense.AmountInclVat, expense.Reinvoiceable, expense.InvoiceId);
    }
}

using PropertyAdmin.Logic.V1.Expenses.Create;
using PropertyAdmin.Logic.V1.Expenses.Models;

namespace PropertyAdmin.Api.V1.Expenses.Create;

public static class Mapping
{
    public static Command ToCommand(this Request request) => new(new ExpenseSaveModel(
        request.AssetId, request.Date, request.Description, request.Category, request.AmountExclVat, request.VatRateName, request.Reinvoiceable));

    public static Response ToResponse(this Result result)
    {
        var expense = result.Expense;
        return new Response(expense.Id, expense.AssetId, expense.AssetName, expense.Date, expense.Description, expense.Category, expense.AmountExclVat,
            expense.VatRateName, expense.VatPercentage, expense.VatAmount, expense.AmountInclVat, expense.Reinvoiceable, expense.InvoiceId);
    }
}

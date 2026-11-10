using PropertyAdmin.Logic.V1.Expenses.Models;

namespace PropertyAdmin.Logic.V1.Expenses.Create;

public sealed record Command(ExpenseSaveModel Expense);

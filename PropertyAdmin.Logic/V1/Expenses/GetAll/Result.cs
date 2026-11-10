using PropertyAdmin.Logic.V1.Expenses.Models;

namespace PropertyAdmin.Logic.V1.Expenses.GetAll;

public sealed record Result(IReadOnlyList<ExpenseModel> Expenses);

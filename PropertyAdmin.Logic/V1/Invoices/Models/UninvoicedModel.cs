using PropertyAdmin.Logic.V1.Activities.Models;
using PropertyAdmin.Logic.V1.Expenses.Models;

namespace PropertyAdmin.Logic.V1.Invoices.Models;

public sealed record UninvoicedModel(IReadOnlyList<ActivityModel> Activities, IReadOnlyList<ExpenseModel> Expenses);

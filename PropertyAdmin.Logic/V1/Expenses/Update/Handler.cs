namespace PropertyAdmin.Logic.V1.Expenses.Update;

public sealed class UpdateHandler(IExpenseService expenses)
{
    public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken) =>
        new(await expenses.UpdateAsync(command.Id, command.Expense, cancellationToken));
}

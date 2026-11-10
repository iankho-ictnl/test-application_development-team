namespace PropertyAdmin.Logic.V1.Expenses.Create;

public sealed class CreateHandler(IExpenseService expenses)
{
    public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken) =>
        new(await expenses.CreateAsync(command.Expense, cancellationToken));
}

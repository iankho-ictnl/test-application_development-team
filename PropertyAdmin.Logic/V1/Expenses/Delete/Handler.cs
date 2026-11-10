namespace PropertyAdmin.Logic.V1.Expenses.Delete;

public sealed class DeleteHandler(IExpenseService expenses)
{
    public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken) =>
        new(await expenses.DeleteAsync(command.Id, cancellationToken));
}

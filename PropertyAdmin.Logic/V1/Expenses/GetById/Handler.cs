namespace PropertyAdmin.Logic.V1.Expenses.GetById;

public sealed class GetByIdHandler(IExpenseService expenses)
{
    public async Task<Result> HandleAsync(Query query, CancellationToken cancellationToken) =>
        new(await expenses.GetByIdAsync(query.Id, query.OwnerScopeId, cancellationToken));
}

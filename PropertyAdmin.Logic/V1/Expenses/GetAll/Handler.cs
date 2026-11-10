namespace PropertyAdmin.Logic.V1.Expenses.GetAll;

public sealed class GetAllHandler(IExpenseService expenses)
{
    public async Task<Result> HandleAsync(Query query, CancellationToken cancellationToken) =>
        new(await expenses.GetAllAsync(query.OwnerScopeId, query.AssetId, query.From, query.To, cancellationToken));
}

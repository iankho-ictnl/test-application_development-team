namespace PropertyAdmin.Logic.V1.Reports.GetAssetResults;

public sealed class GetAssetResultsHandler(IReportService reports)
{
    public async Task<Result> HandleAsync(Query query, CancellationToken cancellationToken) =>
        new(await reports.GetAssetResultsAsync(query.From, query.To, query.OwnerScopeId, cancellationToken));
}

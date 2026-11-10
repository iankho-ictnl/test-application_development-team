namespace PropertyAdmin.Logic.V1.Reports.GetOwnerOverview;

public sealed class GetOwnerOverviewHandler(IReportService reports)
{
    public async Task<Result> HandleAsync(Query query, CancellationToken cancellationToken) =>
        new(await reports.GetOwnerOverviewAsync(query.From, query.To, query.OwnerScopeId, cancellationToken));
}

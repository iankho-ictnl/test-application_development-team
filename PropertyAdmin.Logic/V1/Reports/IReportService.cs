using PropertyAdmin.Logic.V1.Reports.Models;

namespace PropertyAdmin.Logic.V1.Reports;

public interface IReportService
{
    Task<IReadOnlyList<AssetResultModel>> GetAssetResultsAsync(DateOnly from, DateOnly to, int? ownerScopeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OwnerOverviewModel>> GetOwnerOverviewAsync(DateOnly from, DateOnly to, int? ownerScopeId, CancellationToken cancellationToken);
}

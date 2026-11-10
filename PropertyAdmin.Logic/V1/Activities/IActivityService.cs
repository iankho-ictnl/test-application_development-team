using PropertyAdmin.Logic.V1.Activities.Models;

namespace PropertyAdmin.Logic.V1.Activities;

public interface IActivityService
{
    Task<IReadOnlyList<ActivityModel>> GetAllAsync(int? ownerScopeId, int? assetId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken);
    Task<ActivityModel?> GetByIdAsync(int id, int? ownerScopeId, CancellationToken cancellationToken);
    Task<ActivityModel> CreateAsync(ActivitySaveModel model, CancellationToken cancellationToken);
    Task<ActivityModel?> UpdateAsync(int id, ActivitySaveModel model, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}

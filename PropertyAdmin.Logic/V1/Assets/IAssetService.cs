using PropertyAdmin.Logic.V1.Assets.Models;

namespace PropertyAdmin.Logic.V1.Assets;

public interface IAssetService
{
    Task<IReadOnlyList<AssetModel>> GetAllAsync(int? ownerScopeId, int? ownerId, CancellationToken cancellationToken);
    Task<AssetModel?> GetByIdAsync(int id, int? ownerScopeId, CancellationToken cancellationToken);
    Task<AssetModel> CreateAsync(AssetSaveModel model, CancellationToken cancellationToken);
    Task<AssetModel?> UpdateAsync(int id, AssetSaveModel model, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
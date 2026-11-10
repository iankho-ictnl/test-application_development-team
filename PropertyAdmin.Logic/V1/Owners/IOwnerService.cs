using PropertyAdmin.Logic.V1.Owners.Models;

namespace PropertyAdmin.Logic.V1.Owners;

public interface IOwnerService
{
    Task<IReadOnlyList<OwnerModel>> GetAllAsync(int? ownerId, CancellationToken cancellationToken);
    Task<OwnerModel?> GetByIdAsync(int id, int? ownerId, CancellationToken cancellationToken);
    Task<OwnerModel> CreateAsync(OwnerSaveModel model, CancellationToken cancellationToken);
    Task<OwnerModel?> UpdateAsync(int id, OwnerSaveModel model, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
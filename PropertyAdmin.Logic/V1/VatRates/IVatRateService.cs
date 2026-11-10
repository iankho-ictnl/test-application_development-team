using PropertyAdmin.Logic.V1.VatRates.Models;

namespace PropertyAdmin.Logic.V1.VatRates;

public interface IVatRateService
{
    Task<IReadOnlyList<VatRateModel>> GetAllAsync(CancellationToken cancellationToken);
    Task<VatRateModel?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<VatRateModel> CreateAsync(VatRateSaveModel model, CancellationToken cancellationToken);
    Task<VatRateModel?> UpdateAsync(int id, VatRateSaveModel model, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
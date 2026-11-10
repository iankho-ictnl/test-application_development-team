using PropertyAdmin.Logic.V1.VatRates.Models;

namespace PropertyAdmin.Logic.V1.VatRates.GetAll;

public sealed record Result(IReadOnlyList<VatRateModel> Rates);
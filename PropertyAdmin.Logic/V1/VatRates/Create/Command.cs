using PropertyAdmin.Logic.V1.VatRates.Models;

namespace PropertyAdmin.Logic.V1.VatRates.Create;

public sealed record Command(VatRateSaveModel Rate);
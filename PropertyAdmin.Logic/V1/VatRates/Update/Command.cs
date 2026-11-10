using PropertyAdmin.Logic.V1.VatRates.Models;

namespace PropertyAdmin.Logic.V1.VatRates.Update;

public sealed record Command(int Id, VatRateSaveModel Rate);
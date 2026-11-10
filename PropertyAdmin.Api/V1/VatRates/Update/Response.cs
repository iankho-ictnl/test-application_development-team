namespace PropertyAdmin.Api.V1.VatRates.Update;

public sealed record Response(int Id, string Name, decimal Percentage, DateOnly ValidFrom, DateOnly? ValidTo);
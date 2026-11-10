namespace PropertyAdmin.Logic.V1.VatRates.Models;

public sealed record VatRateModel(int Id, string Name, decimal Percentage, DateOnly ValidFrom, DateOnly? ValidTo);
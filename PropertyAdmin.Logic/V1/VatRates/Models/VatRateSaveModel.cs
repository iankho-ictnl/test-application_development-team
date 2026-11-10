namespace PropertyAdmin.Logic.V1.VatRates.Models;

public sealed record VatRateSaveModel(string Name, decimal Percentage, DateOnly ValidFrom, DateOnly? ValidTo);
namespace PropertyAdmin.Api.V1.Expenses.GetAll;

public sealed record Request(int? AssetId, DateOnly? From, DateOnly? To);

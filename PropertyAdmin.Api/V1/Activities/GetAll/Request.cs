namespace PropertyAdmin.Api.V1.Activities.GetAll;

public sealed record Request(int? AssetId, DateOnly? From, DateOnly? To);

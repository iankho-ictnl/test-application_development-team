namespace PropertyAdmin.Logic.V1.Auth.Models;

public sealed record AccessTokenModel(string Token, DateTime ExpiresUtc);

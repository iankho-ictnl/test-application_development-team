namespace PropertyAdmin.Logic.V1.Auth.Models;

public sealed record TokenSubjectModel(
    string UserId,
    string UserName,
    string SecurityStamp,
    int? OwnerId,
    IReadOnlyList<string> Roles,
    IReadOnlyCollection<string> Permissions);

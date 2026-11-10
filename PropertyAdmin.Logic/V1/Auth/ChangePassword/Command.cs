namespace PropertyAdmin.Logic.V1.Auth.ChangePassword;

public sealed record Command(string UserId, string CurrentPassword, string NewPassword);

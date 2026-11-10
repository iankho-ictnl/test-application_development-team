namespace PropertyAdmin.Logic.V1.Auth.ChangePassword;

public sealed class ChangePasswordHandler(IAuthService auth)
{
    public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken) =>
        new(await auth.ChangePasswordAsync(command.UserId, command.CurrentPassword, command.NewPassword, cancellationToken));
}

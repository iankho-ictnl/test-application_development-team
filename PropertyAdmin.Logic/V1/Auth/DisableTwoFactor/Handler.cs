namespace PropertyAdmin.Logic.V1.Auth.DisableTwoFactor;

public sealed class DisableTwoFactorHandler(IAuthService auth)
{
    public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken) =>
        new(await auth.DisableTwoFactorAsync(command.UserId, command.Password, command.Code, cancellationToken));
}

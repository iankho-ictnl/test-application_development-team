namespace PropertyAdmin.Logic.V1.Auth.EnableTwoFactor;

public sealed class EnableTwoFactorHandler(IAuthService auth)
{
    public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken) =>
        new(await auth.EnableTwoFactorAsync(command.UserId, command.Code, cancellationToken));
}

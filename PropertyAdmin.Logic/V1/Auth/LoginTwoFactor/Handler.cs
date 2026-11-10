namespace PropertyAdmin.Logic.V1.Auth.LoginTwoFactor;

public sealed class LoginTwoFactorHandler(IAuthService auth)
{
    public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken) =>
        new(await auth.LoginTwoFactorAsync(command.ChallengeToken, command.Code, cancellationToken));
}

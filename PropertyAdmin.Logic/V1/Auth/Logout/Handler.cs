namespace PropertyAdmin.Logic.V1.Auth.Logout;

public sealed class LogoutHandler(IAuthService auth)
{
    public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken) =>
        new(await auth.LogoutAsync(command.UserId, cancellationToken));
}

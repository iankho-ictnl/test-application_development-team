namespace PropertyAdmin.Logic.V1.Auth.Login;

public sealed class LoginHandler(IAuthService auth)
{
    public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken) =>
        new(await auth.LoginAsync(command.UserName, command.Password, cancellationToken));
}

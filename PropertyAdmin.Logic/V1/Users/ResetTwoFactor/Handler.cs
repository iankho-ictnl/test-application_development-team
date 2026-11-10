namespace PropertyAdmin.Logic.V1.Users.ResetTwoFactor;

public sealed class ResetTwoFactorHandler(IUserService users)
{
    public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken) =>
        new(await users.ResetTwoFactorAsync(command.Id, cancellationToken));
}

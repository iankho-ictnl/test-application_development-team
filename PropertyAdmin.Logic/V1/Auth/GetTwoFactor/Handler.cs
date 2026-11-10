namespace PropertyAdmin.Logic.V1.Auth.GetTwoFactor;

public sealed class GetTwoFactorHandler(IAuthService auth)
{
    public async Task<Result> HandleAsync(Query query, CancellationToken cancellationToken) =>
        new(await auth.GetTwoFactorAsync(query.UserId, cancellationToken));
}

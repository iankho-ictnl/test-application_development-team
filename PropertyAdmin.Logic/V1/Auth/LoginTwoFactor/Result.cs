using PropertyAdmin.Logic.V1.Auth.Models;

namespace PropertyAdmin.Logic.V1.Auth.LoginTwoFactor;

/// <summary>Login is null when the challenge or the code is not valid.</summary>
public sealed record Result(LoginModel? Login);

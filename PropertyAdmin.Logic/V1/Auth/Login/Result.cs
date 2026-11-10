using PropertyAdmin.Logic.V1.Auth.Models;

namespace PropertyAdmin.Logic.V1.Auth.Login;

/// <summary>Login is null when the sign-in failed.</summary>
public sealed record Result(LoginModel? Login);

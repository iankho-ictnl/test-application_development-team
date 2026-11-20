using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyAdmin.Api.Auth;
using PropertyAdmin.Api.V1.Auth.ChangePassword;
using PropertyAdmin.Api.V1.Auth.DisableTwoFactor;
using PropertyAdmin.Api.V1.Auth.EnableTwoFactor;
using PropertyAdmin.Api.V1.Auth.GetTwoFactor;
using PropertyAdmin.Api.V1.Auth.Login;
using PropertyAdmin.Api.V1.Auth.LoginTwoFactor;
using PropertyAdmin.Logic.V1.Auth.ChangePassword;
using PropertyAdmin.Logic.V1.Auth.DisableTwoFactor;
using PropertyAdmin.Logic.V1.Auth.EnableTwoFactor;
using PropertyAdmin.Logic.V1.Auth.GetTwoFactor;
using PropertyAdmin.Logic.V1.Auth.Login;
using PropertyAdmin.Logic.V1.Auth.LoginTwoFactor;
using PropertyAdmin.Logic.V1.Auth.Logout;
using GetTwoFactorMapping = PropertyAdmin.Api.V1.Auth.GetTwoFactor.Mapping;
using LogoutMapping = PropertyAdmin.Api.V1.Auth.Logout.Mapping;

namespace PropertyAdmin.Api.V1.Auth;

/// <summary>Sign-in (anonymous) and the caller's own account (signed-in user only, the fallback policy).</summary>
[ApiController]
[Route("api/v1/auth")]
[Route("api/auth")] // Legacy alias (Q-045): remove after T-037 and T-038.
public sealed class AuthController : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<Login.Response>> Login(
        Login.Request request,
        [FromServices] LoginHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.ToCommand(), cancellationToken);
        return result.Login is null ? Unauthorized() : Ok(result.ToResponse());
    }

    [AllowAnonymous]
    [HttpPost("login-2fa")]
    public async Task<ActionResult<LoginTwoFactor.Response>> LoginTwoFactor(
        LoginTwoFactor.Request request,
        [FromServices] LoginTwoFactorHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.ToCommand(), cancellationToken);
        return result.Login is null ? Unauthorized() : Ok(result.ToResponse());
    }

    [HttpGet("2fa")]
    public async Task<ActionResult<GetTwoFactor.Response>> GetTwoFactor(
        [FromServices] GetTwoFactorHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(GetTwoFactorMapping.ToQuery(CurrentUserId), cancellationToken);
        return result.Setup is null ? Unauthorized() : Ok(result.ToResponse());
    }

    [HttpPost("2fa/enable")]
    public async Task<ActionResult<EnableTwoFactor.Response>> EnableTwoFactor(
        EnableTwoFactor.Request request,
        [FromServices] EnableTwoFactorHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.ToCommand(CurrentUserId), cancellationToken);
        return result.RecoveryCodes is null ? Unauthorized() : Ok(result.ToResponse());
    }

    [HttpPost("2fa/disable")]
    public async Task<IActionResult> DisableTwoFactor(
        DisableTwoFactor.Request request,
        [FromServices] DisableTwoFactorHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.ToCommand(CurrentUserId), cancellationToken);
        return result.Found ? NoContent() : Unauthorized();
    }

    [HttpPost("password")]
    public async Task<IActionResult> ChangePassword(
        ChangePassword.Request request,
        [FromServices] ChangePasswordHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.ToCommand(CurrentUserId), cancellationToken);
        return result.Found ? NoContent() : Unauthorized();
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromServices] LogoutHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(LogoutMapping.ToCommand(CurrentUserId), cancellationToken);
        return result.Found ? NoContent() : Unauthorized();
    }

    private string CurrentUserId => User.UserId() ?? "";
}

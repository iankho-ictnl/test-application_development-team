using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyAdmin.Api.V1.Users.Create;
using PropertyAdmin.Api.V1.Users.GetById;
using PropertyAdmin.Api.V1.Users.Update;
using PropertyAdmin.Contracts;
using PropertyAdmin.Logic.V1.Users.Create;
using PropertyAdmin.Logic.V1.Users.Delete;
using PropertyAdmin.Logic.V1.Users.ForceSignOut;
using PropertyAdmin.Logic.V1.Users.GetAll;
using PropertyAdmin.Logic.V1.Users.GetById;
using PropertyAdmin.Logic.V1.Users.ResetTwoFactor;
using PropertyAdmin.Logic.V1.Users.Update;
using DeleteMapping = PropertyAdmin.Api.V1.Users.Delete.Mapping;
using ForceSignOutMapping = PropertyAdmin.Api.V1.Users.ForceSignOut.Mapping;
using GetAllMapping = PropertyAdmin.Api.V1.Users.GetAll.Mapping;
using GetByIdMapping = PropertyAdmin.Api.V1.Users.GetById.Mapping;
using ResetTwoFactorMapping = PropertyAdmin.Api.V1.Users.ResetTwoFactor.Mapping;

namespace PropertyAdmin.Api.V1.Users;

[ApiController]
[Route("api/v1/users")]
[Authorize(Policy = Permissions.Users)]
public sealed class UsersController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GetAll.Response>>> GetAll(
        [FromServices] GetAllHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(GetAllMapping.ToQuery(), cancellationToken);
        return Ok(GetAllMapping.ToResponse(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GetById.Response>> GetById(
        string id,
        [FromServices] GetByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(GetByIdMapping.ToQuery(id), cancellationToken);
        return result.User is null ? NotFound() : Ok(result.ToResponse());
    }

    [HttpPost]
    public async Task<ActionResult<Create.Response>> Create(
        Create.Request request,
        [FromServices] CreateHandler handler,
        CancellationToken cancellationToken)
    {
        var response = (await handler.HandleAsync(request.ToCommand(), cancellationToken)).ToResponse();
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Update.Response>> Update(
        string id,
        Update.Request request,
        [FromServices] UpdateHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.ToCommand(id), cancellationToken);
        return result.User is null ? NotFound() : Ok(result.ToResponse());
    }

    [HttpPost("{id}/reset-2fa")]
    public async Task<IActionResult> ResetTwoFactor(
        string id,
        [FromServices] ResetTwoFactorHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(ResetTwoFactorMapping.ToCommand(id), cancellationToken);
        return result.Found ? NoContent() : NotFound();
    }

    [HttpPost("{id}/sign-out")]
    public async Task<IActionResult> ForceSignOut(
        string id,
        [FromServices] ForceSignOutHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(ForceSignOutMapping.ToCommand(id), cancellationToken);
        return result.Found ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(
        string id,
        [FromServices] DeleteHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(DeleteMapping.ToCommand(id), cancellationToken);
        return result.Found ? NoContent() : NotFound();
    }
}

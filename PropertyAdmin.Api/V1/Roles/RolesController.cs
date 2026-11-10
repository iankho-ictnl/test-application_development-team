using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyAdmin.Api.V1.Roles.Create;
using PropertyAdmin.Api.V1.Roles.Update;
using PropertyAdmin.Contracts;
using PropertyAdmin.Logic.V1.Roles.Create;
using PropertyAdmin.Logic.V1.Roles.Delete;
using PropertyAdmin.Logic.V1.Roles.GetAll;
using PropertyAdmin.Logic.V1.Roles.Update;
using DeleteMapping = PropertyAdmin.Api.V1.Roles.Delete.Mapping;
using GetAllMapping = PropertyAdmin.Api.V1.Roles.GetAll.Mapping;

namespace PropertyAdmin.Api.V1.Roles;

[ApiController]
[Route("api/v1/roles")]
[Route("api/roles")] // Legacy alias (Q-045): remove after T-037 and T-038.
[Authorize(Policy = Permissions.Users)]
public sealed class RolesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GetAll.Response>>> GetAll(
        [FromServices] GetAllHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(GetAllMapping.ToQuery(), cancellationToken);
        return Ok(GetAllMapping.ToResponse(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        Create.Request request,
        [FromServices] CreateHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(request.ToCommand(), cancellationToken);
        return NoContent();
    }

    [HttpPut("{name}")]
    public async Task<IActionResult> Update(
        string name,
        Update.Request request,
        [FromServices] UpdateHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.ToCommand(name), cancellationToken);
        return result.Found ? NoContent() : NotFound();
    }

    [HttpDelete("{name}")]
    public async Task<IActionResult> Delete(
        string name,
        [FromServices] DeleteHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(DeleteMapping.ToCommand(name), cancellationToken);
        return result.Found ? NoContent() : NotFound();
    }
}

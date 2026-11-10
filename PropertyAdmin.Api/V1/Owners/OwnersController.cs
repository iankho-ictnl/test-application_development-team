using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyAdmin.Api.Auth;
using PropertyAdmin.Api.V1.Owners.Create;
using PropertyAdmin.Api.V1.Owners.Delete;
using PropertyAdmin.Api.V1.Owners.GetById;
using PropertyAdmin.Api.V1.Owners.GetAll;
using PropertyAdmin.Api.V1.Owners.Update;
using PropertyAdmin.Logic.V1.Owners.Create;
using PropertyAdmin.Logic.V1.Owners.Delete;
using PropertyAdmin.Logic.V1.Owners.GetById;
using PropertyAdmin.Logic.V1.Owners.GetAll;
using PropertyAdmin.Logic.V1.Owners.Update;
using OwnersGetAllRequest = PropertyAdmin.Api.V1.Owners.GetAll.Request;
using OwnersGetAllResponse = PropertyAdmin.Api.V1.Owners.GetAll.Response;
using OwnersGetByIdQuery = PropertyAdmin.Logic.V1.Owners.GetById.Query;
using OwnersDeleteCommand = PropertyAdmin.Logic.V1.Owners.Delete.Command;

namespace PropertyAdmin.Api.V1.Owners;

[ApiController]
[Route("api/v1/owners")]
[Route("api/owners")] // Legacy alias (Q-045): remove after T-037 and T-038.
public sealed class OwnersController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.Read)]
    public async Task<ActionResult<IReadOnlyList<OwnersGetAllResponse>>> GetAll(
        [FromQuery] OwnersGetAllRequest request,
        [FromServices] GetAllHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.ToQuery(User.ReadScopeOwnerId()), cancellationToken);
        return Ok(result.ToResponse());
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = Policies.Read)]
    public async Task<ActionResult<GetById.Response>> GetById(
        int id,
        [FromServices] GetByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new OwnersGetByIdQuery(id, User.ReadScopeOwnerId()), cancellationToken);
        return result.Owner is null ? NotFound() : Ok(result.ToResponse());
    }

    [HttpPost]
    [Authorize(Policy = Permissions.OwnersAssets)]
    public async Task<ActionResult<Create.Response>> Create(
        Create.Request request,
        [FromServices] CreateHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.ToCommand(), cancellationToken);
        var response = result.ToResponse();
        return this.CreatedUnderRequestPath(response.Id, response);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.OwnersAssets)]
    public async Task<ActionResult<Update.Response>> Update(
        int id,
        Update.Request request,
        [FromServices] UpdateHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.ToCommand(id), cancellationToken);
        return result.Owner is null ? NotFound() : Ok(result.ToResponse());
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.OwnersAssets)]
    public async Task<IActionResult> Delete(
        int id,
        [FromServices] DeleteHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new OwnersDeleteCommand(id), cancellationToken);
        return result.Deleted ? NoContent() : NotFound();
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyAdmin.Api.Auth;
using PropertyAdmin.Api.V1.Assets.GetAll;
using PropertyAdmin.Api.V1.Assets.GetById;
using PropertyAdmin.Api.V1.Assets.Create;
using PropertyAdmin.Api.V1.Assets.Update;
using PropertyAdmin.Contracts;
using PropertyAdmin.Logic.V1.Assets.GetAll;
using PropertyAdmin.Logic.V1.Assets.GetById;
using PropertyAdmin.Logic.V1.Assets.Create;
using PropertyAdmin.Logic.V1.Assets.Update;
using PropertyAdmin.Logic.V1.Assets.Delete;
using AssetsGetAllRequest = PropertyAdmin.Api.V1.Assets.GetAll.Request;
using AssetsGetAllResponse = PropertyAdmin.Api.V1.Assets.GetAll.Response;
using AssetsGetByIdRequest = PropertyAdmin.Api.V1.Assets.GetById.Request;
using AssetsGetByIdResponse = PropertyAdmin.Api.V1.Assets.GetById.Response;
using AssetsDeleteCommand = PropertyAdmin.Logic.V1.Assets.Delete.Command;

namespace PropertyAdmin.Api.V1.Assets;

[ApiController]
[Route("api/v1/assets")]
[Route("api/assets")] // Legacy alias (Q-045): remove after T-037 and T-038.
public sealed class AssetsController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.Read)]
    public async Task<ActionResult<IReadOnlyList<AssetsGetAllResponse>>> GetAll(
        [FromQuery] AssetsGetAllRequest request,
        [FromServices] GetAllHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.ToQuery(User.ReadScopeOwnerId()), cancellationToken);
        return Ok(result.ToResponse());
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = Policies.Read)]
    public async Task<ActionResult<AssetsGetByIdResponse>> GetById(
        int id,
        [FromServices] GetByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new PropertyAdmin.Logic.V1.Assets.GetById.Query(id, User.ReadScopeOwnerId()), cancellationToken);
        return result.Asset is null ? NotFound() : Ok(result.ToResponse());
    }

    [HttpPost]
    [Authorize(Policy = Permissions.OwnersAssets)]
    public async Task<ActionResult<PropertyAdmin.Api.V1.Assets.Create.Response>> Create(
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
    public async Task<ActionResult<PropertyAdmin.Api.V1.Assets.Update.Response>> Update(
        int id,
        Update.Request request,
        [FromServices] UpdateHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.ToCommand(id), cancellationToken);
        return result.Asset is null ? NotFound() : Ok(result.ToResponse());
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.OwnersAssets)]
    public async Task<IActionResult> Delete(
        int id,
        [FromServices] DeleteHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new AssetsDeleteCommand(id), cancellationToken);
        return result.Deleted ? NoContent() : NotFound();
    }
}
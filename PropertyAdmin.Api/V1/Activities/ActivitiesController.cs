using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyAdmin.Api.Auth;
using PropertyAdmin.Api.V1.Activities.Create;
using PropertyAdmin.Api.V1.Activities.Delete;
using PropertyAdmin.Api.V1.Activities.GetAll;
using PropertyAdmin.Api.V1.Activities.GetById;
using PropertyAdmin.Api.V1.Activities.Update;
using PropertyAdmin.Contracts;
using PropertyAdmin.Logic.V1.Activities.Create;
using PropertyAdmin.Logic.V1.Activities.Delete;
using PropertyAdmin.Logic.V1.Activities.GetAll;
using PropertyAdmin.Logic.V1.Activities.GetById;
using PropertyAdmin.Logic.V1.Activities.Update;

namespace PropertyAdmin.Api.V1.Activities;

[ApiController]
[Route("api/v1/activities")]
public sealed class ActivitiesController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.Read)]
    public async Task<ActionResult<IReadOnlyList<GetAll.Response>>> GetAll(
        [FromQuery] GetAll.Request request,
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
        var result = await handler.HandleAsync(new GetById.Request(id).ToQuery(User.ReadScopeOwnerId()), cancellationToken);
        return result.Activity is null ? NotFound() : Ok(result.ToResponse());
    }

    [HttpPost]
    [Authorize(Policy = Permissions.ExpensesActivities)]
    public async Task<ActionResult<Create.Response>> Create(
        Create.Request request,
        [FromServices] CreateHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.ToCommand(), cancellationToken);
        var response = result.ToResponse();
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.ExpensesActivities)]
    public async Task<ActionResult<Update.Response>> Update(
        int id,
        Update.Request request,
        [FromServices] UpdateHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.ToCommand(id), cancellationToken);
        return result.Activity is null ? NotFound() : Ok(result.ToResponse());
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.ExpensesActivities)]
    public async Task<IActionResult> Delete(
        int id,
        [FromServices] DeleteHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new Delete.Request(id).ToCommand(), cancellationToken);
        return result.Deleted ? NoContent() : NotFound();
    }
}

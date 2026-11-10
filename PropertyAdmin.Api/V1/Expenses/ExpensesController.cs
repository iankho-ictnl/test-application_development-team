using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyAdmin.Api.Auth;
using PropertyAdmin.Api.V1.Expenses.Create;
using PropertyAdmin.Api.V1.Expenses.Delete;
using PropertyAdmin.Api.V1.Expenses.GetAll;
using PropertyAdmin.Api.V1.Expenses.GetById;
using PropertyAdmin.Api.V1.Expenses.Update;
using PropertyAdmin.Logic.V1.Expenses.Create;
using PropertyAdmin.Logic.V1.Expenses.Delete;
using PropertyAdmin.Logic.V1.Expenses.GetAll;
using PropertyAdmin.Logic.V1.Expenses.GetById;
using PropertyAdmin.Logic.V1.Expenses.Update;

namespace PropertyAdmin.Api.V1.Expenses;

[ApiController]
[Route("api/v1/expenses")]
[Route("api/expenses")] // Legacy alias (Q-045): remove after T-037 and T-038.
public sealed class ExpensesController : ControllerBase
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
        return result.Expense is null ? NotFound() : Ok(result.ToResponse());
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
        return this.CreatedUnderRequestPath(response.Id, response);
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
        return result.Expense is null ? NotFound() : Ok(result.ToResponse());
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

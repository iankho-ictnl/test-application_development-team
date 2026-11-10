using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyAdmin.Api.Auth;
using PropertyAdmin.Api.V1.Invoices.Create;
using PropertyAdmin.Api.V1.Invoices.Delete;
using PropertyAdmin.Api.V1.Invoices.GetAll;
using PropertyAdmin.Api.V1.Invoices.GetById;
using PropertyAdmin.Api.V1.Invoices.GetPdf;
using PropertyAdmin.Api.V1.Invoices.GetUninvoiced;
using PropertyAdmin.Api.V1.Invoices.MarkPaid;
using PropertyAdmin.Contracts;
using PropertyAdmin.Logic.V1.Invoices.Create;
using PropertyAdmin.Logic.V1.Invoices.Delete;
using PropertyAdmin.Logic.V1.Invoices.GetAll;
using PropertyAdmin.Logic.V1.Invoices.GetById;
using PropertyAdmin.Logic.V1.Invoices.GetPdf;
using PropertyAdmin.Logic.V1.Invoices.GetUninvoiced;
using PropertyAdmin.Logic.V1.Invoices.MarkPaid;

namespace PropertyAdmin.Api.V1.Invoices;

[ApiController]
[Route("api/v1/invoices")]
public sealed class InvoicesController : ControllerBase
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
        return result.Invoice is null ? NotFound() : Ok(result.ToResponse());
    }

    [HttpGet("{id:int}/pdf")]
    [Authorize(Policy = Policies.Read)]
    public async Task<IActionResult> GetPdf(
        int id,
        [FromServices] GetPdfHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetPdf.Request(id).ToQuery(User.ReadScopeOwnerId()), cancellationToken);
        return result.Found ? File(result.Content!, "application/pdf", result.FileName) : NotFound();
    }

    [HttpGet("uninvoiced/{ownerId:int}")]
    [Authorize(Policy = Permissions.Invoices)]
    public async Task<ActionResult<GetUninvoiced.Response>> GetUninvoiced(
        int ownerId,
        [FromServices] GetUninvoicedHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetUninvoiced.Request(ownerId).ToQuery(), cancellationToken);
        return Ok(result.ToResponse());
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Invoices)]
    public async Task<ActionResult<Create.Response>> Create(
        Create.Request request,
        [FromServices] CreateHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.ToCommand(), cancellationToken);
        var response = result.ToResponse();
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPost("{id:int}/pay")]
    [Authorize(Policy = Permissions.Invoices)]
    public async Task<IActionResult> MarkPaid(
        int id,
        MarkPaid.Request request,
        [FromServices] MarkPaidHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.ToCommand(id), cancellationToken);
        return result.Found ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.Invoices)]
    public async Task<IActionResult> Delete(
        int id,
        [FromServices] DeleteHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new Delete.Request(id).ToCommand(), cancellationToken);
        return result.Deleted ? NoContent() : NotFound();
    }
}

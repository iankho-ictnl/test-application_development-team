using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyAdmin.Api.Auth;
using PropertyAdmin.Api.V1.VatRates.GetAll;
using PropertyAdmin.Api.V1.VatRates.GetById;
using PropertyAdmin.Api.V1.VatRates.Create;
using PropertyAdmin.Api.V1.VatRates.Update;
using PropertyAdmin.Logic.V1.VatRates.GetAll;
using PropertyAdmin.Logic.V1.VatRates.GetById;
using PropertyAdmin.Logic.V1.VatRates.Create;
using PropertyAdmin.Logic.V1.VatRates.Update;
using PropertyAdmin.Logic.V1.VatRates.Delete;
using VatRatesGetAllRequest = PropertyAdmin.Api.V1.VatRates.GetAll.Request;
using VatRatesGetAllResponse = PropertyAdmin.Api.V1.VatRates.GetAll.Response;
using VatRatesGetByIdRequest = PropertyAdmin.Api.V1.VatRates.GetById.Request;
using VatRatesGetByIdResponse = PropertyAdmin.Api.V1.VatRates.GetById.Response;
using VatRatesDeleteCommand = PropertyAdmin.Logic.V1.VatRates.Delete.Command;

namespace PropertyAdmin.Api.V1.VatRates;

[ApiController]
[Route("api/v1/vatrates")]
[Route("api/vatrates")] // Legacy alias (Q-045): remove after T-037 and T-038.
public sealed class VatRatesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VatRatesGetAllResponse>>> GetAll(
        [FromServices] GetAllHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new PropertyAdmin.Logic.V1.VatRates.GetAll.Query(), cancellationToken);
        return Ok(result.ToResponse());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VatRatesGetByIdResponse>> GetById(
        int id,
        [FromServices] GetByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new PropertyAdmin.Logic.V1.VatRates.GetById.Query(id), cancellationToken);
        return result.Rate is null ? NotFound() : Ok(result.ToResponse());
    }

    [HttpPost]
    [Authorize(Policy = Permissions.VatRates)]
    public async Task<ActionResult<PropertyAdmin.Api.V1.VatRates.Create.Response>> Create(
        Create.Request request,
        [FromServices] CreateHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.ToCommand(), cancellationToken);
        var response = result.ToResponse();
        return this.CreatedUnderRequestPath(response.Id, response);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.VatRates)]
    public async Task<ActionResult<PropertyAdmin.Api.V1.VatRates.Update.Response>> Update(
        int id,
        Update.Request request,
        [FromServices] UpdateHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.ToCommand(id), cancellationToken);
        return result.Rate is null ? NotFound() : Ok(result.ToResponse());
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.VatRates)]
    public async Task<IActionResult> Delete(
        int id,
        [FromServices] DeleteHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new VatRatesDeleteCommand(id), cancellationToken);
        return result.Deleted ? NoContent() : NotFound();
    }
}
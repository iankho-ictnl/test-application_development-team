using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyAdmin.Api.Auth;
using PropertyAdmin.Api.V1.Reports.GetAssetResults;
using PropertyAdmin.Api.V1.Reports.GetOwnerOverview;
using PropertyAdmin.Logic.V1.Reports.GetAssetResults;
using PropertyAdmin.Logic.V1.Reports.GetOwnerOverview;

namespace PropertyAdmin.Api.V1.Reports;

[ApiController]
[Route("api/v1/reports")]
[Route("api/reports")] // Legacy alias (Q-045): remove after T-037 and T-038.
[Authorize(Policy = Permissions.Reports)]
public sealed class ReportsController : ControllerBase
{
    [HttpGet("asset-results")]
    public async Task<ActionResult<IReadOnlyList<GetAssetResults.Response>>> GetAssetResults(
        [FromQuery] GetAssetResults.Request request,
        [FromServices] GetAssetResultsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.ToQuery(User.ReadScopeOwnerId()), cancellationToken);
        return Ok(result.ToResponse());
    }

    [HttpGet("owner-overview")]
    public async Task<ActionResult<IReadOnlyList<GetOwnerOverview.Response>>> GetOwnerOverview(
        [FromQuery] GetOwnerOverview.Request request,
        [FromServices] GetOwnerOverviewHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.ToQuery(User.ReadScopeOwnerId()), cancellationToken);
        return Ok(result.ToResponse());
    }
}

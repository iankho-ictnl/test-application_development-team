using Microsoft.AspNetCore.Mvc;

namespace PropertyAdmin.Api;

public static class ControllerBaseExtensions
{
    /// <summary>201 with a Location under the route the request came in on (v1 or the legacy alias of Q-045).</summary>
    public static CreatedResult CreatedUnderRequestPath(this ControllerBase controller, object id, object? value)
    {
        var request = controller.Request;
        var path = (request.PathBase + request.Path).Value!.TrimEnd('/');
        return controller.Created($"{path}/{Uri.EscapeDataString(id.ToString()!)}", value);
    }
}

using Microsoft.Extensions.DependencyInjection;
using PropertyAdmin.Data;

namespace PropertyAdmin.Logic;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the logic layer: per feature the service and per use case the handler, as plain
    /// classes (no MediatR, D-026). The features are added under V1/ by T-033 to T-036.
    /// </summary>
    public static IServiceCollection AddLogic(this IServiceCollection services, string? connectionString)
    {
        services.AddData(connectionString);
        return services;
    }
}

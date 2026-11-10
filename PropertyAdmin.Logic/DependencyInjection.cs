using Microsoft.Extensions.DependencyInjection;
using PropertyAdmin.Data;
using PropertyAdmin.Logic.V1.Owners;
using PropertyAdmin.Logic.V1.Owners.Create;
using PropertyAdmin.Logic.V1.Owners.Delete;
using PropertyAdmin.Logic.V1.Owners.GetById;
using PropertyAdmin.Logic.V1.Owners.GetAll;
using PropertyAdmin.Logic.V1.Owners.Update;

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
        services.AddScoped<IOwnerService, OwnerService>();
        services.AddScoped<CreateHandler>();
        services.AddScoped<DeleteHandler>();
        services.AddScoped<GetByIdHandler>();
        services.AddScoped<GetAllHandler>();
        services.AddScoped<UpdateHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Assets.IAssetService, PropertyAdmin.Logic.V1.Assets.AssetService>();
        services.AddScoped<PropertyAdmin.Logic.V1.Assets.GetAll.GetAllHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Assets.GetById.GetByIdHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Assets.Create.CreateHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Assets.Update.UpdateHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Assets.Delete.DeleteHandler>();
        return services;
    }
}

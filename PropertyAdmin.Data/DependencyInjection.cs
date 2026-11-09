using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace PropertyAdmin.Data;

public static class DependencyInjection
{
    /// <summary>Registers the database context on PostgreSQL. Only PropertyAdmin.Logic uses it (D-026).</summary>
    public static IServiceCollection AddData(this IServiceCollection services, string? connectionString)
    {
        services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));
        return services;
    }

    /// <summary>Connects ASP.NET Core Identity to the database context.</summary>
    public static IdentityBuilder AddDataStores(this IdentityBuilder identity)
        => identity.AddEntityFrameworkStores<AppDbContext>();
}

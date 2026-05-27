using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using notX.Application.Interfaces;
using notX.Application.Interfaces.Repositories;
using notX.Infrastructure.Persistence.Connections;
using notX.Infrastructure.Persistence.Repositories;

namespace notX.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(configuration);

        services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();

        services.AddScoped<IApplicationRepository, ApplicationRepository>();

        return services;
    }
}

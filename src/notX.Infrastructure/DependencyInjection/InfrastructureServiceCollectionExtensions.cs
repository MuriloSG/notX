using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using notX.Application.Interfaces;
using notX.Application.Interfaces.Repositories;
using notX.Infrastructure.Persistence.Connections;
using notX.Infrastructure.Persistence.Repositories;
using notX.Infrastructure.Settings;

namespace notX.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(configuration);

        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
        services.Configure<TwilioSettings>(configuration.GetSection(TwilioSettings.SectionName));

        services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();

        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        return services;
    }
}

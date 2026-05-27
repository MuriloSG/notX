using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;

namespace notX.Infrastructure.Persistence.Extensions;

public static class MigrationExtensions
{
    public static void ApplyMigrations(string connectionString)
    {
        var serviceProvider = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb =>
                rb.AddPostgres()
                  .WithGlobalConnectionString(connectionString)
                  .ScanIn(typeof(MigrationExtensions).Assembly).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole())
            .BuildServiceProvider(false);

        using var scope = serviceProvider.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

        runner.MigrateUp();
    }
}
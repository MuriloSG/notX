using notX.EmailWorker;
using notX.Infrastructure.DependencyInjection;
using notX.Infrastructure.Persistence.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

if (builder.Environment.IsDevelopment())
{
    var connectionString = builder.Configuration.GetConnectionString("notxdb");
    if (!string.IsNullOrWhiteSpace(connectionString))
        MigrationExtensions.ApplyMigrations(connectionString);
}

host.Run();

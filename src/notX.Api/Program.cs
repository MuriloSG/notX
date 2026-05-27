using notX.Application.DependencyInjection;
using notX.Infrastructure.DependencyInjection;
using notX.Infrastructure.Persistence.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    var connectionString = app.Configuration.GetConnectionString("notxdb");

    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        MigrationExtensions.ApplyMigrations(connectionString);
    }

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger"))
    .ExcludeFromDescription();

app.MapControllers();

app.MapDefaultEndpoints();

app.Run();
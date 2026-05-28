using System.Text.Json.Serialization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using notX.Application.DependencyInjection;
using notX.Application.Interfaces;
using notX.Api.Middleware;
using notX.Api.Services;
using notX.Infrastructure.DependencyInjection;
using notX.Infrastructure.Persistence.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((doc, _, _) =>
    {
        doc.Info = new OpenApiInfo
        {
            Title = "notX API",
            Version = "v1",
            Description = "Plataforma de envio de notificações via Email e SMS."
        };

        doc.Components ??= new OpenApiComponents();
        doc.Components.SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>
        {
            ["ApiKey"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = "X-Api-Key",
                Description = "Informe a API key da sua aplicação no header X-Api-Key. Obrigatório em todos os endpoints de /notifications."
            }
        };

        doc.SecurityRequirements =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" } }] = []
            }
        ];

        return Task.CompletedTask;
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<CurrentApplication>();
builder.Services.AddScoped<ICurrentApplication>(sp => sp.GetRequiredService<CurrentApplication>());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    var connectionString = app.Configuration.GetConnectionString("notxdb");

    if (!string.IsNullOrWhiteSpace(connectionString))
        MigrationExtensions.ApplyMigrations(connectionString);

    app.MapOpenApi();

    app.MapScalarApiReference(options =>
    {
        options.Title = "notX API";
        options.Theme = ScalarTheme.DeepSpace;
        options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
        options.Authentication = new ScalarAuthenticationOptions
        {
            PreferredSecuritySchemes = ["ApiKey"]
        };
    });
}

app.UseHttpsRedirection();

app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();

app.UseMiddleware<ApiKeyMiddleware>();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();

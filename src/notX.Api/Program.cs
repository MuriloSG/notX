using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using notX.Application.DependencyInjection;
using notX.Application.Interfaces;
using notX.Api.Middleware;
using notX.Api.Services;
using notX.Infrastructure.DependencyInjection;
using notX.Infrastructure.Persistence.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "notX API",
        Version = "v1",
        Description = "Plataforma de envio de notificações via Email e SMS."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    options.UseInlineDefinitionsForEnums();

    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Name = "X-Api-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Description = "Informe a API key da sua aplicação no header X-Api-Key. Obrigatório em todos os endpoints de /notifications."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            []
        }
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

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "notX API v1");
        c.DisplayRequestDuration();
    });
}

app.UseHttpsRedirection();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.UseMiddleware<ApiKeyMiddleware>();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();

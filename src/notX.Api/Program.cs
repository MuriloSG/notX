using System.Reflection;
using Microsoft.OpenApi.Models;
using notX.Application.DependencyInjection;
using notX.Application.Interfaces;
using notX.Api.Middleware;
using notX.Api.Services;
using notX.Infrastructure.DependencyInjection;
using notX.Infrastructure.Persistence.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "notX API",
        Version = "v1",
        Description = "Platform for sending notifications via Email, SMS, Push and Webhook."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Name = "X-Api-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Description = "Provide your application API key in the X-Api-Key header. Required for all /notifications endpoints."
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

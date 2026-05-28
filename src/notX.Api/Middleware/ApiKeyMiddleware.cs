using notX.Api.Services;
using notX.Application.Interfaces.Repositories;

namespace notX.Api.Middleware;

public sealed class ApiKeyMiddleware(RequestDelegate next)
{
    private const string ApiKeyHeader = "X-Api-Key";

    private static readonly string[] ExcludedPrefixes = ["/applications", "/health", "/alive", "/scalar", "/openapi"];

    public async Task InvokeAsync(HttpContext context, IApplicationRepository repository, CurrentApplication currentApplication)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (path == "/" || ExcludedPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out var apiKeyValue) ||
            string.IsNullOrWhiteSpace(apiKeyValue))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { Code = "Auth.MissingApiKey", Message = "Missing X-Api-Key header." });
            return;
        }

        var application = await repository.GetByApiKeyAsync(apiKeyValue!);

        if (application is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { Code = "Auth.InvalidApiKey", Message = "Invalid API key." });
            return;
        }

        currentApplication.Set(application.Id, application.ApiKey);

        await next(context);
    }
}

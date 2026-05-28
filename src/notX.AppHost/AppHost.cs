var builder = DistributedApplication.CreateBuilder(args);

var postgresPassword = builder.AddParameter("postgres-password", "postgres", secret: true);

var postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithDataVolume();

var database = postgres.AddDatabase("notxdb");

var redis = builder.AddRedis("redis")
    .WithDataVolume();

builder.AddProject<Projects.notX_Api>("notx-api")
    .WithReference(database)
    .WithReference(redis)
    .WaitFor(database)
    .WaitFor(redis)
    .WithUrls(ctx =>
    {
        // Renomeia os endpoints existentes (http/https) — eles já apontam pra raiz, que serve a SPA
        foreach (var url in ctx.Urls.Where(u => u.Endpoint is not null))
            url.DisplayText = $"Dashboard ({url.Endpoint!.EndpointName})";

        // Adiciona um link separado pro Scalar usando a base do endpoint https (ou http como fallback)
        var baseUrl = ctx.Urls.FirstOrDefault(u => u.Endpoint?.EndpointName == "https")?.Url
                   ?? ctx.Urls.FirstOrDefault(u => u.Endpoint?.EndpointName == "http")?.Url;
        if (baseUrl is not null)
            ctx.Urls.Add(new() { Url = $"{baseUrl.TrimEnd('/')}/scalar/v1", DisplayText = "API Docs (Scalar)" });
    });

builder.AddProject<Projects.notX_EmailWorker>("notx-emailworker")
    .WithReference(database)
    .WithReference(redis)
    .WaitFor(database)
    .WaitFor(redis)
    .WithEnvironment("Smtp__Host", builder.Configuration["SMTP_HOST"] ?? "smtp.gmail.com")
    .WithEnvironment("Smtp__Port", builder.Configuration["SMTP_PORT"] ?? "587")
    .WithEnvironment("Smtp__EnableSsl", builder.Configuration["SMTP_ENABLE_SSL"] ?? "true")
    .WithEnvironment("Smtp__Username", builder.Configuration["SMTP_USERNAME"] ?? "")
    .WithEnvironment("Smtp__Password", builder.Configuration["SMTP_PASSWORD"] ?? "")
    .WithEnvironment("Smtp__FromName", builder.Configuration["SMTP_FROM_NAME"] ?? "notX")
    .WithEnvironment("Smtp__FromEmail", builder.Configuration["SMTP_FROM_EMAIL"] ?? "");

builder.Build().Run();

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
    .WaitFor(redis);

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

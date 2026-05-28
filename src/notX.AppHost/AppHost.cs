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
    .WithEnvironment("Smtp__Host", Environment.GetEnvironmentVariable("Smtp__Host") ?? "smtp.gmail.com")
    .WithEnvironment("Smtp__Port", Environment.GetEnvironmentVariable("Smtp__Port") ?? "587")
    .WithEnvironment("Smtp__EnableSsl", Environment.GetEnvironmentVariable("Smtp__EnableSsl") ?? "true")
    .WithEnvironment("Smtp__Username", Environment.GetEnvironmentVariable("Smtp__Username") ?? "")
    .WithEnvironment("Smtp__Password", Environment.GetEnvironmentVariable("Smtp__Password") ?? "")
    .WithEnvironment("Smtp__FromName", Environment.GetEnvironmentVariable("Smtp__FromName") ?? "notX")
    .WithEnvironment("Smtp__FromEmail", Environment.GetEnvironmentVariable("Smtp__FromEmail") ?? "");

builder.Build().Run();

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

builder.Build().Run();

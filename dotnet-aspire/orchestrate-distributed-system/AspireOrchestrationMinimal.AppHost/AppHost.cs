var builder = DistributedApplication.CreateBuilder(args);

var api = builder
    .AddProject<Projects.AspireOrchestrationMinimal_Api>("api");

builder
    .AddProject<Projects.AspireOrchestrationMinimal_Web>("web")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
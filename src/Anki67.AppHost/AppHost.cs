var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Anki67_Api>("api", launchProfileName: "http")
    .WithEndpoint("http", endpoint => endpoint.IsProxied = false)
    .WithHttpHealthCheck("/api/health");

builder.AddProject<Projects.Anki67_Web>("web", launchProfileName: "http")
    .WithExternalHttpEndpoints()
    .WithEndpoint("http", endpoint => endpoint.IsProxied = false)
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();

using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var seq = builder
    .AddSeq("seq")
        .WithHttpEndpoint(port: 5341, targetPort: 80, name: "seqlog");

builder
    .AddProject<Projects.RetroGamingWebAPI>("retrogamingwebapi")
        .WithReplicas(1)
        .WithHttpEndpoint(port: 5000, name: "retrogamingwebapi")
        .WithHttpsEndpoint(port: 5001, name: "retrogamingwebapi-secure")
        .WithHttpEndpoint(port: 5002, name: "management")

        .WithEnvironment("HEALTH_INITIAL_STATE", "Degraded")
    .WithReference(seq);

builder.Build().Run();
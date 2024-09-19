var builder = DistributedApplication.CreateBuilder(args);

// Add Projects with Dapr pub sub
var pubSub = builder.AddDaprPubSub("pubsub");

builder.AddProject<Projects.Frontend>("frontend");
builder.AddProject<Projects.Backend>("backend")
    .WithDaprSidecar()
    .WithReference(pubSub);
builder.AddProject<Projects.WeatherGenerator>("weather-generator")
    .WithDaprSidecar()
    .WithReference(pubSub);

builder.Build().Run();
var builder = DistributedApplication.CreateBuilder(args);

// Add Projects with Dapr pub sub
var pubSub = builder.AddDaprPubSub("pubsub");

builder.AddProject<Projects.Publisher>("publisher")
    .WithDaprSidecar()
    .WithReference(pubSub);
builder.AddProject<Projects.Subscriber>("subscriber")
    .WithDaprSidecar()
    .WithReference(pubSub);

builder.Build().Run();
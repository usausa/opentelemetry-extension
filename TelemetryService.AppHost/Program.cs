var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.TelemetryService>("telemetry-service");

builder.Build().Run();

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.SensorTelemetryService>("sensor-telemetry-service");

builder.Build().Run();

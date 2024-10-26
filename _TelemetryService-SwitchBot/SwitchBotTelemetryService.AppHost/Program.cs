var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.SwitchBotTelemetryService>("switchbot-telemetry-service");

builder.Build().Run();

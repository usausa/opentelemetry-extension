using OpenTelemetry.Metrics;

using OpenTelemetryExtension.Instrumentation.HardwareMonitor;
using OpenTelemetryExtension.Instrumentation.SensorOmron;

using Serilog;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

var builder = Host.CreateApplicationBuilder(args);

// Service
builder.Services
    .AddWindowsService()
    .AddSystemd();

// Logging
builder.Logging.ClearProviders();
builder.Services.AddSerilog(options =>
{
    options.ReadFrom.Configuration(builder.Configuration);
});

// Metrics
builder.Services
    .AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddHardwareMonitorInstrumentation()
            .AddSensorOmronInstrumentation("COM12");

        // http://localhost:9464/metrics
        metrics.AddPrometheusHttpListener();
    });

var host = builder.Build();

host.Run();

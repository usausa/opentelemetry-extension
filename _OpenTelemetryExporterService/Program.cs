using OpenTelemetry.Metrics;

using OpenTelemetryExporterService.Metrics;
using OpenTelemetryExporterService.Settings;

using OpenTelemetryExtension.Instrumentation.HardwareMonitor;
using OpenTelemetryExtension.Instrumentation.SensorOmron;

using Serilog;

using OpenTelemetryExporterService;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

// Builder
var builder = Host.CreateApplicationBuilder(args);

// Setting
var setting = builder.Configuration.GetSection("Service").Get<ServiceSetting>()!;

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
        if (setting.UseApplicationMetrics)
        {
            metrics.AddApplicationInstrumentation();
        }

        if (setting.UseHardwareMetrics)
        {
            metrics.AddHardwareMonitorInstrumentation(setting.Hardware);
        }

        if (setting.UseSensorMetrics)
        {
            metrics.AddSensorOmronInstrumentation(setting.Sensor);
        }

        metrics.AddPrometheusHttpListener(options =>
        {
            options.UriPrefixes = setting.EndPoints;
        });
    });

// Build
var host = builder.Build();

// Startup
var log = host.Services.GetRequiredService<ILogger<Program>>();
log.InfoServiceStart();
log.InfoServiceSettingsEnvironment(typeof(Program).Assembly.GetName().Version, Environment.Version, Environment.CurrentDirectory);
log.InfoServiceSettingsMetrics(setting.UseApplicationMetrics, setting.UseHardwareMetrics, setting.UseSensorMetrics);

host.Run();

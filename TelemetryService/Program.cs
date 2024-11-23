using System.Runtime.InteropServices;

using OpenTelemetry.Metrics;
using OpenTelemetry;

using OpenTelemetryExtension.Instrumentation.Ping;

#if WINDOWS_TELEMETRY
using OpenTelemetryExtension.Instrumentation.Ble;
#endif
#if WINDOWS_TELEMETRY
using OpenTelemetryExtension.Instrumentation.DiskInfo;
#endif
#if WINDOWS_TELEMETRY
using OpenTelemetryExtension.Instrumentation.HardwareMonitor;
#endif
#if WINDOWS_TELEMETRY
using OpenTelemetryExtension.Instrumentation.HyperV;
#endif
#if WINDOWS_TELEMETRY
using OpenTelemetryExtension.Instrumentation.PerformanceCounter;
#endif
using OpenTelemetryExtension.Instrumentation.SensorOmron;
#if WINDOWS_TELEMETRY
using OpenTelemetryExtension.Instrumentation.SwitchBot;
#endif
using OpenTelemetryExtension.Instrumentation.WFWattch2;
#if WINDOWS_TELEMETRY
using OpenTelemetryExtension.Instrumentation.Wifi;
#endif

using Serilog;

using TelemetryService;
using TelemetryService.Settings;
using TelemetryService.Metrics;

// Builder
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var builder = Host.CreateApplicationBuilder(args);
var useOtlpExporter = !String.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

// Setting
var setting = builder.Configuration.GetSection("Telemetry").Get<TelemetrySetting>()!;

// Service
builder.Services
    .AddWindowsService()
    .AddSystemd();

// Logging
builder.Logging.ClearProviders();
builder.Services.AddSerilog(options =>
{
    options.ReadFrom.Configuration(builder.Configuration);
}, writeToProviders: useOtlpExporter);

// Metrics
builder.Services
    .AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        var host = setting.Host ?? Environment.MachineName;
        var instrumentationList = new List<string>();

#if WINDOWS_TELEMETRY
        if (setting.EnableBleMetrics)
        {
            setting.Ble.Host = String.IsNullOrWhiteSpace(setting.Ble.Host) ? host : setting.Ble.Host;
            metrics.AddBleInstrumentation(setting.Ble);
            instrumentationList.Add(nameof(setting.Ble));
        }
#endif
#if WINDOWS_TELEMETRY
        if (setting.EnableDiskInfoMetrics)
        {
            setting.DiskInfo.Host = String.IsNullOrWhiteSpace(setting.DiskInfo.Host) ? host : setting.DiskInfo.Host;
            metrics.AddDiskInfoInstrumentation(setting.DiskInfo);
            instrumentationList.Add(nameof(setting.DiskInfo));
        }
#endif
#if WINDOWS_TELEMETRY
        if (setting.EnableHardwareMonitorMetrics)
        {
            setting.HardwareMonitor.Host = String.IsNullOrWhiteSpace(setting.HardwareMonitor.Host) ? host : setting.HardwareMonitor.Host;
            metrics.AddHardwareMonitorInstrumentation(setting.HardwareMonitor);
            instrumentationList.Add(nameof(setting.HardwareMonitor));
        }
#endif
#if WINDOWS_TELEMETRY
        if (setting.EnableHyperVMetrics)
        {
            setting.HyperV.Host = String.IsNullOrWhiteSpace(setting.HyperV.Host) ? host : setting.HyperV.Host;
            metrics.AddHyperVInstrumentation(setting.HyperV);
            instrumentationList.Add(nameof(setting.HyperV));
        }
#endif
#if WINDOWS_TELEMETRY
        if (setting.EnablePerformanceCounterMetrics)
        {
            setting.PerformanceCounter.Host = String.IsNullOrWhiteSpace(setting.PerformanceCounter.Host) ? host : setting.PerformanceCounter.Host;
            metrics.AddPerformanceCounterInstrumentation(setting.PerformanceCounter);
            instrumentationList.Add(nameof(setting.PerformanceCounter));
        }
#endif
        if (setting.EnablePingMetrics)
        {
            setting.Ping.Host = String.IsNullOrWhiteSpace(setting.Ping.Host) ? host : setting.Ping.Host;
            metrics.AddPingInstrumentation(setting.Ping);
            instrumentationList.Add(nameof(setting.Ping));
        }
        if (setting.EnableSensorOmronMetrics)
        {
            metrics.AddSensorOmronInstrumentation(setting.SensorOmron);
            instrumentationList.Add(nameof(setting.SensorOmron));
        }
        if (setting.EnableWFWattch2Metrics)
        {
            metrics.AddWFWattch2Instrumentation(setting.WFWattch2);
            instrumentationList.Add(nameof(setting.WFWattch2));
        }
#if WINDOWS_TELEMETRY
        if (setting.EnableSwitchBotMetrics)
        {
            metrics.AddSwitchBotInstrumentation(setting.SwitchBot);
            instrumentationList.Add(nameof(setting.SwitchBot));
        }
#endif
#if WINDOWS_TELEMETRY
        if (setting.EnableWifiMetrics)
        {
            setting.Wifi.Host = String.IsNullOrWhiteSpace(setting.Wifi.Host) ? host : setting.Wifi.Host;
            metrics.AddWifiInstrumentation(setting.Wifi);
            instrumentationList.Add(nameof(setting.Wifi));
        }
#endif

        if (setting.EnableApplicationMetrics)
        {
            metrics.AddApplicationInstrumentation(new ApplicationOptions
            {
                Host = host,
                InstrumentationList = [.. instrumentationList]
            });
        }

        metrics.AddPrometheusHttpListener(options =>
        {
            options.UriPrefixes = setting.EndPoints;
            options.DisableTotalNameSuffixForCounters = true;
        });
        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }
    });

// Build
var host = builder.Build();

// Startup
var log = host.Services.GetRequiredService<ILogger<Program>>();
log.InfoServiceStart();
log.InfoServiceSettingsRuntime(RuntimeInformation.OSDescription, RuntimeInformation.FrameworkDescription, RuntimeInformation.RuntimeIdentifier);
log.InfoServiceSettingsEnvironment(typeof(Program).Assembly.GetName().Version, Environment.CurrentDirectory);

host.Run();

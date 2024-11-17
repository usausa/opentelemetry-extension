namespace TelemetryService.Settings;

#if WINDOWS_TELEMETRY
using OpenTelemetryExtension.Instrumentation.DiskInfo;
#endif
#if WINDOWS_TELEMETRY
using OpenTelemetryExtension.Instrumentation.HardwareMonitor;
#endif
#if WINDOWS_TELEMETRY
using OpenTelemetryExtension.Instrumentation.PerformanceCounter;
#endif
using OpenTelemetryExtension.Instrumentation.SensorOmron;
#if WINDOWS_TELEMETRY
using OpenTelemetryExtension.Instrumentation.SwitchBot.Windows;
#endif
using OpenTelemetryExtension.Instrumentation.Ping;
using OpenTelemetryExtension.Instrumentation.WFWattch2;

#pragma warning disable CA1819
public sealed class TelemetrySetting
{
    public string[] EndPoints { get; set; } = default!;

    public string? Host { get; set; }

    // Enable

    public bool EnableApplicationMetrics { get; set; }

#if WINDOWS_TELEMETRY
    public bool EnableHardwareMetrics { get; set; }
#endif

#if WINDOWS_TELEMETRY
    public bool EnableDiskInfoMetrics { get; set; }
#endif

#if WINDOWS_TELEMETRY
    public bool EnablePerformanceCounterMetrics { get; set; }
#endif

    public bool EnablePingMetrics { get; set; }

    public bool EnableSensorOmronMetrics { get; set; }

#if WINDOWS_TELEMETRY
    public bool EnableSwitchBotMetrics { get; set; }
#endif

    public bool EnableWFWattch2Metrics { get; set; }

    // Option

#if WINDOWS_TELEMETRY
    public HardwareMonitorOptions HardwareMonitor { get; set; } = new();
#endif

#if WINDOWS_TELEMETRY
    public DiskInfoOptions DiskInfo { get; set; } = new();
#endif

#if WINDOWS_TELEMETRY
    public PerformanceCounterOptions PerformanceCounter { get; set; } = new();
#endif

    public PingOptions Ping { get; set; } = new();

    public SensorOmronOptions SensorOmron { get; set; } = new();

#if WINDOWS_TELEMETRY
    public SwitchBotOptions SwitchBot { get; set; } = new();
#endif

    public WFWattch2Options WFWattch2 { get; set; } = new();
}
#pragma warning restore CA1819

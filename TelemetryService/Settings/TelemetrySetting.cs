namespace TelemetryService.Settings;

#if WINDOWS_TELEMETRY
using OpenTelemetryExtension.Instrumentation.HardwareMonitor;
#endif
using OpenTelemetryExtension.Instrumentation.SensorOmron;
#if WINDOWS_TELEMETRY
using OpenTelemetryExtension.Instrumentation.SwitchBot.Windows;
#endif

#pragma warning disable CA1819
public sealed class TelemetrySetting
{
    public string[] EndPoints { get; set; } = default!;

    public bool EnableApplicationMetrics { get; set; }

#if WINDOWS_TELEMETRY
    public bool EnableHardwareMetrics { get; set; }
#endif

    public bool EnableSensorOmronMetrics { get; set; }

#if WINDOWS_TELEMETRY
    public bool EnableSwitchBotMetrics { get; set; }
#endif

#if WINDOWS_TELEMETRY
    public HardwareMonitorOptions Hardware { get; set; } = default!;
#endif

    public SensorOmronOptions SensorOmron { get; set; } = default!;

#if WINDOWS_TELEMETRY
    public SwitchBotOptions SwitchBot { get; set; } = default!;
#endif
}
#pragma warning restore CA1819

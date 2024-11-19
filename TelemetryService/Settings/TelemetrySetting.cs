namespace TelemetryService.Settings;

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
using OpenTelemetryExtension.Instrumentation.PerformanceCounter;
#endif
using OpenTelemetryExtension.Instrumentation.SensorOmron;
#if WINDOWS_TELEMETRY
using OpenTelemetryExtension.Instrumentation.SwitchBot;
#endif
using OpenTelemetryExtension.Instrumentation.Ping;
using OpenTelemetryExtension.Instrumentation.WFWattch2;
#if WINDOWS_TELEMETRY
using OpenTelemetryExtension.Instrumentation.Wifi;
#endif

#pragma warning disable CA1819
public sealed class TelemetrySetting
{
    // Application

    public string[] EndPoints { get; set; } = default!;

    public string? Host { get; set; }

    public bool EnableApplicationMetrics { get; set; }

    // Ble

#if WINDOWS_TELEMETRY
    public bool EnableBleMetrics { get; set; }

    public BleOptions Ble { get; set; } = new();
#endif

    // DiskInfo

#if WINDOWS_TELEMETRY
    public bool EnableDiskInfoMetrics { get; set; }

    public DiskInfoOptions DiskInfo { get; set; } = new();
#endif

    // HardwareMonitor

#if WINDOWS_TELEMETRY
    public bool EnableHardwareMetrics { get; set; }

    public HardwareMonitorOptions HardwareMonitor { get; set; } = new();
#endif

    // PerformanceCounter

#if WINDOWS_TELEMETRY
    public bool EnablePerformanceCounterMetrics { get; set; }

    public PerformanceCounterOptions PerformanceCounter { get; set; } = new();
#endif

    // Ping

    public bool EnablePingMetrics { get; set; }

    public PingOptions Ping { get; set; } = new();

    // SensorOmron

    public bool EnableSensorOmronMetrics { get; set; }

    public SensorOmronOptions SensorOmron { get; set; } = new();

    // SwitchBot

#if WINDOWS_TELEMETRY
    public bool EnableSwitchBotMetrics { get; set; }

    public SwitchBotOptions SwitchBot { get; set; } = new();
#endif

    // WFWattch2

    public bool EnableWFWattch2Metrics { get; set; }

    public WFWattch2Options WFWattch2 { get; set; } = new();

    // Wifi

#if WINDOWS_TELEMETRY
    public bool EnableWifiMetrics { get; set; }

    public WifiOptions Wifi { get; set; } = new();
#endif
}
#pragma warning restore CA1819

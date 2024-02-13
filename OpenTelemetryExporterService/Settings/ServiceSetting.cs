namespace OpenTelemetryExporterService.Settings;

using OpenTelemetryExtension.Instrumentation.HardwareMonitor;
using OpenTelemetryExtension.Instrumentation.SensorOmron;

public sealed class ServiceSetting
{
    public string[] EndPoints { get; set; } = default!;

    public bool UseApplicationMetrics { get; set; }

    public bool UseHardwareMetrics { get; set; }

    public bool UseSensorMetrics { get; set; }

    public HardwareMonitorInstrumentationOptions Hardware { get; set; } = default!;

    public SensorOmronInstrumentationOptions Sensor { get; set; } = default!;
}

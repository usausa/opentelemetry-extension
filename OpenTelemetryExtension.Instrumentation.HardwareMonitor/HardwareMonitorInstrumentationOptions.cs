namespace OpenTelemetryExtension.Instrumentation.HardwareMonitor;

public sealed class HardwareMonitorInstrumentationOptions
{
    public int Interval { get; set; } = 10000;

    public bool IsBatteryEnabled { get; set; } = true;

    public bool IsControllerEnabled { get; set; } = true;

    public bool IsCpuEnabled { get; set; } = true;

    public bool IsGpuEnabled { get; set; } = true;

    public bool IsMemoryEnabled { get; set; } = true;

    public bool IsMotherboardEnabled { get; set; } = true;

    public bool IsNetworkEnabled { get; set; } = true;

    //public bool IsPsuEnabled { get; set; } = true;

    public bool IsStorageEnabled { get; set; } = true;
}

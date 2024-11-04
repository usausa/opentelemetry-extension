namespace OpenTelemetryExtension.Instrumentation.DiskInfo;

public sealed class DiskInfoOptions
{
    public int Interval { get; set; } = 5000;

    public string? Host { get; set; }
}

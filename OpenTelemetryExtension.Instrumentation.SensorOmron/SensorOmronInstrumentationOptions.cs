namespace OpenTelemetryExtension.Instrumentation.SensorOmron;

public sealed class SensorOmronInstrumentationOptions
{
    public string Port { get; set; } = default!;

    public int Interval { get; set; } = 5000;
}

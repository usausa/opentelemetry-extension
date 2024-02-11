namespace OpenTelemetryExtension.Instrumentation.SensorOmron;

public sealed class SensorOmronInstrumentationOptions
{
    public string Port { get; set; } = default!;

    public int Interval { get; set; } = 10000;

    public bool IsTemperatureEnabled { get; set; } = true;

    public bool IsHumidityEnabled { get; set; } = true;

    public bool IsLightEnabled { get; set; } = true;

    public bool IsPressureEnabled { get; set; } = true;

    public bool IsNoiseEnabled { get; set; } = true;

    public bool IsDiscomfortEnabled { get; set; } = true;

    public bool IsHeatEnabled { get; set; } = true;

    public bool IsEtvocEnabled { get; set; } = true;

    public bool IsEco2Enabled { get; set; } = true;

    public bool IsSeismicEnabled { get; set; } = true;
}

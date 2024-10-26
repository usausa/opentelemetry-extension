namespace SensorTelemetryService;

#pragma warning disable CA1819
public sealed class ServiceSetting
{
    public string[] EndPoints { get; set; } = default!;

    public string Port { get; set; } = default!;
}
#pragma warning restore CA1819

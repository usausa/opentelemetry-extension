namespace TelemetryService.Metrics;

internal sealed class ApplicationOptions
{
    public string Host { get; set; } = default!;

    public string[] InstrumentationList { get; set; } = default!;
}

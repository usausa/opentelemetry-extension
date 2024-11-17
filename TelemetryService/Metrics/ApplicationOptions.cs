namespace TelemetryService.Metrics;

internal class ApplicationOptions
{
    public string Host { get; set; } = default!;

    public string[] InstrumentationList { get; set; } = default!;
}

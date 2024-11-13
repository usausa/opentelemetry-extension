namespace TelemetryService.Metrics;

using OpenTelemetry.Metrics;

internal static class MeterProviderBuilderExtensions
{
    public static MeterProviderBuilder AddApplicationInstrumentation(this MeterProviderBuilder builder)
    {
        builder.AddMeter(ApplicationMetrics.MeterName);
        return builder.AddInstrumentation(static p => new ApplicationMetrics(p.GetRequiredService<ILogger<ApplicationMetrics>>()));
    }
}

namespace TelemetryService.Metrics;

using OpenTelemetry.Metrics;

internal static class MeterProviderBuilderExtensions
{
    public static MeterProviderBuilder AddApplicationInstrumentation(this MeterProviderBuilder builder, ApplicationOptions options)
    {
        builder.AddMeter(ApplicationMetrics.MeterName);
        return builder.AddInstrumentation(p => new ApplicationMetrics(p.GetRequiredService<ILogger<ApplicationMetrics>>(), options));
    }
}

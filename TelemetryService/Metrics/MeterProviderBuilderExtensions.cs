namespace TelemetryService.Metrics;

using OpenTelemetry.Metrics;

public static class MeterProviderBuilderExtensions
{
    public static MeterProviderBuilder AddApplicationInstrumentation(this MeterProviderBuilder builder)
    {
        builder.AddMeter(ApplicationMetrics.MeterName);
        return builder.AddInstrumentation(p => new ApplicationMetrics(p.GetRequiredService<ILogger<ApplicationMetrics>>()));
    }
}

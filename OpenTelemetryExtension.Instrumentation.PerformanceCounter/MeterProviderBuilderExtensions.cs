namespace OpenTelemetryExtension.Instrumentation.PerformanceCounter;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OpenTelemetry.Metrics;

public static class MeterProviderBuilderExtensions
{
    public static MeterProviderBuilder AddPerformanceCounterInstrumentation(this MeterProviderBuilder builder) =>
        AddPerformanceCounterInstrumentation(builder, static _ => { });

    public static MeterProviderBuilder AddPerformanceCounterInstrumentation(this MeterProviderBuilder builder, Action<PerformanceCounterOptions> configure)
    {
        var options = new PerformanceCounterOptions();
        configure(options);

        builder.AddMeter(PerformanceCounterMetrics.MeterName);
        return builder.AddInstrumentation(p => new PerformanceCounterMetrics(p.GetRequiredService<ILogger<PerformanceCounterMetrics>>(), options));
    }

    public static MeterProviderBuilder AddPerformanceCounterInstrumentation(this MeterProviderBuilder builder, PerformanceCounterOptions options)
    {
        builder.AddMeter(PerformanceCounterMetrics.MeterName);
        return builder.AddInstrumentation(p => new PerformanceCounterMetrics(p.GetRequiredService<ILogger<PerformanceCounterMetrics>>(), options));
    }
}

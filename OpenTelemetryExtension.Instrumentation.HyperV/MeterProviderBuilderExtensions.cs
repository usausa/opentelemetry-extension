namespace OpenTelemetryExtension.Instrumentation.HyperV;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OpenTelemetry.Metrics;

public static class MeterProviderBuilderExtensions
{
    public static MeterProviderBuilder AddHyperVInstrumentation(this MeterProviderBuilder builder) =>
        AddHyperVInstrumentation(builder, static _ => { });

    public static MeterProviderBuilder AddHyperVInstrumentation(this MeterProviderBuilder builder, Action<HyperVOptions> configure)
    {
        var options = new HyperVOptions();
        configure(options);

        builder.AddMeter(HyperVMetrics.MeterName);
        return builder.AddInstrumentation(p => new HyperVMetrics(p.GetRequiredService<ILogger<HyperVMetrics>>(), options));
    }

    public static MeterProviderBuilder AddHyperVInstrumentation(this MeterProviderBuilder builder, HyperVOptions options)
    {
        builder.AddMeter(HyperVMetrics.MeterName);
        return builder.AddInstrumentation(p => new HyperVMetrics(p.GetRequiredService<ILogger<HyperVMetrics>>(), options));
    }
}

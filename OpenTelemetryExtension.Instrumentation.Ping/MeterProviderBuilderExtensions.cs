namespace OpenTelemetryExtension.Instrumentation.Ping;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OpenTelemetry.Metrics;

public static class MeterProviderBuilderExtensions
{
    public static MeterProviderBuilder AddPingInstrumentation(this MeterProviderBuilder builder) =>
        AddPingInstrumentation(builder, static _ => { });

    public static MeterProviderBuilder AddPingInstrumentation(this MeterProviderBuilder builder, Action<PingOptions> configure)
    {
        var options = new PingOptions();
        configure(options);

        builder.AddMeter(PingMetrics.MeterName);
        return builder.AddInstrumentation(p => new PingMetrics(p.GetRequiredService<ILogger<PingMetrics>>(), options));
    }

    public static MeterProviderBuilder AddPingInstrumentation(this MeterProviderBuilder builder, PingOptions options)
    {
        builder.AddMeter(PingMetrics.MeterName);
        return builder.AddInstrumentation(p => new PingMetrics(p.GetRequiredService<ILogger<PingMetrics>>(), options));
    }
}

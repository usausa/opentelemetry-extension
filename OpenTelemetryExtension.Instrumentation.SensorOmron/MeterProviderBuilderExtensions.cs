namespace OpenTelemetryExtension.Instrumentation.SensorOmron;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OpenTelemetry.Metrics;

public static class MeterProviderBuilderExtensions
{
    public static MeterProviderBuilder AddSensorOmronInstrumentation(this MeterProviderBuilder builder, Action<SensorOmronOptions> configure)
    {
        var options = new SensorOmronOptions();
        configure(options);

        builder.AddMeter(SensorOmronMetrics.MeterName);
        return builder.AddInstrumentation(p => new SensorOmronMetrics(p.GetRequiredService<ILogger<SensorOmronMetrics>>(), options));
    }

    public static MeterProviderBuilder AddSensorOmronInstrumentation(this MeterProviderBuilder builder, SensorOmronOptions options)
    {
        builder.AddMeter(SensorOmronMetrics.MeterName);
        return builder.AddInstrumentation(p => new SensorOmronMetrics(p.GetRequiredService<ILogger<SensorOmronMetrics>>(), options));
    }
}

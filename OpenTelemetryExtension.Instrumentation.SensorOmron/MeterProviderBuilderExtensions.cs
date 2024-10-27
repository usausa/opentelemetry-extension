namespace OpenTelemetryExtension.Instrumentation.SensorOmron;

using OpenTelemetry.Metrics;

public static class MeterProviderBuilderExtensions
{
    public static MeterProviderBuilder AddSensorOmronInstrumentation(this MeterProviderBuilder builder, Action<SensorOmronOptions> configure)
    {
        var options = new SensorOmronOptions();
        configure(options);

        builder.AddMeter(SensorOmronMetrics.MeterName);
        return builder.AddInstrumentation(() => new SensorOmronMetrics(options));
    }

    public static MeterProviderBuilder AddSensorOmronInstrumentation(this MeterProviderBuilder builder, SensorOmronOptions options)
    {
        builder.AddMeter(SensorOmronMetrics.MeterName);
        return builder.AddInstrumentation(() => new SensorOmronMetrics(options));
    }
}

namespace OpenTelemetryExtension.Instrumentation.SensorOmron;

using OpenTelemetry.Metrics;

public static class MeterProviderBuilderExtensions
{
    public static MeterProviderBuilder AddSensorOmronInstrumentation(this MeterProviderBuilder builder, string port) =>
        AddSensorOmronInstrumentation(builder, options => { options.Port = port; });

    public static MeterProviderBuilder AddSensorOmronInstrumentation(this MeterProviderBuilder builder, Action<SensorOmronInstrumentationOptions> configure)
    {
        var options = new SensorOmronInstrumentationOptions();
        configure(options);

        builder.AddMeter(SensorOmronMetrics.MeterName);
        return builder.AddInstrumentation(() => new SensorOmronMetrics(options));
    }
}

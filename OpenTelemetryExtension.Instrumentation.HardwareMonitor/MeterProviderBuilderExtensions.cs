namespace OpenTelemetryExtension.Instrumentation.HardwareMonitor;

using OpenTelemetry.Metrics;

public static class MeterProviderBuilderExtensions
{
    public static MeterProviderBuilder AddHardwareMonitorInstrumentation(this MeterProviderBuilder builder) =>
        AddHardwareMonitorInstrumentation(builder, _ => { });

    public static MeterProviderBuilder AddHardwareMonitorInstrumentation(this MeterProviderBuilder builder, Action<HardwareMonitorInstrumentationOptions> configure)
    {
        var options = new HardwareMonitorInstrumentationOptions();
        configure(options);

        builder.AddMeter(HardwareMonitorMetrics.MeterName);
        return builder.AddInstrumentation(() => new HardwareMonitorMetrics(options));
    }

    public static MeterProviderBuilder AddHardwareMonitorInstrumentation(this MeterProviderBuilder builder, HardwareMonitorInstrumentationOptions options)
    {
        builder.AddMeter(HardwareMonitorMetrics.MeterName);
        return builder.AddInstrumentation(() => new HardwareMonitorMetrics(options));
    }
}

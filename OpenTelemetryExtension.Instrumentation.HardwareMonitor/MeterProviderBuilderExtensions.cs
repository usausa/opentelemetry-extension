namespace OpenTelemetryExtension.Instrumentation.HardwareMonitor;

using OpenTelemetry.Metrics;

public static class MeterProviderBuilderExtensions
{
    public static MeterProviderBuilder AddHardwareMonitorInstrumentation(this MeterProviderBuilder builder) =>
        AddHardwareMonitorInstrumentation(builder, _ => { });

    public static MeterProviderBuilder AddHardwareMonitorInstrumentation(this MeterProviderBuilder builder, Action<HardwareMonitorOptions> configure)
    {
        var options = new HardwareMonitorOptions();
        configure(options);

        builder.AddMeter(HardwareMonitorMetrics.MeterName);
        return builder.AddInstrumentation(() => new HardwareMonitorMetrics(options));
    }

    public static MeterProviderBuilder AddHardwareMonitorInstrumentation(this MeterProviderBuilder builder, HardwareMonitorOptions options)
    {
        builder.AddMeter(HardwareMonitorMetrics.MeterName);
        return builder.AddInstrumentation(() => new HardwareMonitorMetrics(options));
    }
}

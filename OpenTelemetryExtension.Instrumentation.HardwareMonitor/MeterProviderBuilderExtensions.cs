namespace OpenTelemetryExtension.Instrumentation.HardwareMonitor;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OpenTelemetry.Metrics;

public static class MeterProviderBuilderExtensions
{
    public static MeterProviderBuilder AddHardwareMonitorInstrumentation(this MeterProviderBuilder builder) =>
        AddHardwareMonitorInstrumentation(builder, static _ => { });

    public static MeterProviderBuilder AddHardwareMonitorInstrumentation(this MeterProviderBuilder builder, Action<HardwareMonitorOptions> configure)
    {
        var options = new HardwareMonitorOptions();
        configure(options);

        builder.AddMeter(HardwareMonitorMetrics.MeterName);
        return builder.AddInstrumentation(p => new HardwareMonitorMetrics(p.GetRequiredService<ILogger<HardwareMonitorMetrics>>(), options));
    }

    public static MeterProviderBuilder AddHardwareMonitorInstrumentation(this MeterProviderBuilder builder, HardwareMonitorOptions options)
    {
        builder.AddMeter(HardwareMonitorMetrics.MeterName);
        return builder.AddInstrumentation(p => new HardwareMonitorMetrics(p.GetRequiredService<ILogger<HardwareMonitorMetrics>>(), options));
    }
}

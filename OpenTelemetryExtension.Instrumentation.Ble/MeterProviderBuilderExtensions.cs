namespace OpenTelemetryExtension.Instrumentation.Ble;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OpenTelemetry.Metrics;

public static class MeterProviderBuilderExtensions
{
    public static MeterProviderBuilder AddBleInstrumentation(this MeterProviderBuilder builder) =>
        AddBleInstrumentation(builder, static _ => { });

    public static MeterProviderBuilder AddBleInstrumentation(this MeterProviderBuilder builder, Action<BleOptions> configure)
    {
        var options = new BleOptions();
        configure(options);

        builder.AddMeter(BleMetrics.MeterName);
        return builder.AddInstrumentation(p => new BleMetrics(p.GetRequiredService<ILogger<BleMetrics>>(), options));
    }

    public static MeterProviderBuilder AddBleInstrumentation(this MeterProviderBuilder builder, BleOptions options)
    {
        builder.AddMeter(BleMetrics.MeterName);
        return builder.AddInstrumentation(p => new BleMetrics(p.GetRequiredService<ILogger<BleMetrics>>(), options));
    }
}

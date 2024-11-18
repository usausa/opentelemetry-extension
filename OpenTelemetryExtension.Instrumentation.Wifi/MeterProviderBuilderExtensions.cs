namespace OpenTelemetryExtension.Instrumentation.Wifi;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OpenTelemetry.Metrics;

public static class MeterProviderBuilderExtensions
{
    public static MeterProviderBuilder AddWifiInstrumentation(this MeterProviderBuilder builder) =>
        AddWifiInstrumentation(builder, static _ => { });

    public static MeterProviderBuilder AddWifiInstrumentation(this MeterProviderBuilder builder, Action<WifiOptions> configure)
    {
        var options = new WifiOptions();
        configure(options);

        builder.AddMeter(WifiMetrics.MeterName);
        return builder.AddInstrumentation(p => new WifiMetrics(p.GetRequiredService<ILogger<WifiMetrics>>(), options));
    }

    public static MeterProviderBuilder AddWifiInstrumentation(this MeterProviderBuilder builder, WifiOptions options)
    {
        builder.AddMeter(WifiMetrics.MeterName);
        return builder.AddInstrumentation(p => new WifiMetrics(p.GetRequiredService<ILogger<WifiMetrics>>(), options));
    }
}

namespace OpenTelemetryExtension.Instrumentation.WFWattch2;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OpenTelemetry.Metrics;

public static class MeterProviderBuilderExtensions
{
    public static MeterProviderBuilder AddWFWattch2Instrumentation(this MeterProviderBuilder builder, Action<WFWattch2Options> configure)
    {
        var options = new WFWattch2Options();
        configure(options);

        builder.AddMeter(WFWattch2Metrics.MeterName);
        return builder.AddInstrumentation(p => new WFWattch2Metrics(p.GetRequiredService<ILogger<WFWattch2Metrics>>(), options));
    }

    public static MeterProviderBuilder AddWFWattch2Instrumentation(this MeterProviderBuilder builder, WFWattch2Options options)
    {
        builder.AddMeter(WFWattch2Metrics.MeterName);
        return builder.AddInstrumentation(p => new WFWattch2Metrics(p.GetRequiredService<ILogger<WFWattch2Metrics>>(), options));
    }
}

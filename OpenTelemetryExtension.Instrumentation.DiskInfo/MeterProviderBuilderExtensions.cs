namespace OpenTelemetryExtension.Instrumentation.DiskInfo;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OpenTelemetry.Metrics;

public static class MeterProviderBuilderExtensions
{
    public static MeterProviderBuilder AddDiskInfoInstrumentation(this MeterProviderBuilder builder) =>
        AddDiskInfoInstrumentation(builder, static _ => { });

    public static MeterProviderBuilder AddDiskInfoInstrumentation(this MeterProviderBuilder builder, Action<DiskInfoOptions> configure)
    {
        var options = new DiskInfoOptions();
        configure(options);

        builder.AddMeter(DiskInfoMetrics.MeterName);
        return builder.AddInstrumentation(p => new DiskInfoMetrics(p.GetRequiredService<ILogger<DiskInfoMetrics>>(), options));
    }

    public static MeterProviderBuilder AddDiskInfoInstrumentation(this MeterProviderBuilder builder, DiskInfoOptions options)
    {
        builder.AddMeter(DiskInfoMetrics.MeterName);
        return builder.AddInstrumentation(p => new DiskInfoMetrics(p.GetRequiredService<ILogger<DiskInfoMetrics>>(), options));
    }
}

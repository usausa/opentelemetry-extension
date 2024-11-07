namespace OpenTelemetryExtension.Instrumentation.PerformanceCounter;

using System.Diagnostics.Metrics;
using System.Reflection;

using Microsoft.Extensions.Logging;

internal sealed class PerformanceCounterMetrics : IDisposable
{
    internal static readonly AssemblyName AssemblyName = typeof(PerformanceCounterMetrics).Assembly.GetName();
    internal static readonly string MeterName = AssemblyName.Name!.Replace(".Instrumentation", string.Empty, StringComparison.Ordinal);

#pragma warning disable CA1823
    // ReSharper disable once UnusedMember.Local
    private static readonly Meter MeterInstance = new(MeterName, AssemblyName.Version!.ToString());

    // ReSharper disable once NotAccessedField.Local
    private readonly string host;
#pragma warning restore CA1823

    public PerformanceCounterMetrics(
        ILogger<PerformanceCounterMetrics> log,
        PerformanceCounterOptions options)
    {
        log.InfoMetricsEnabled(nameof(PerformanceCounterMetrics));

        host = options.Host ?? Environment.MachineName;

        // TODO
    }

    public void Dispose()
    {
        // TODO
    }

    // TODO
}

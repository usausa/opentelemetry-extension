namespace OpenTelemetryExtension.Instrumentation.HyperV;

using System.Diagnostics.Metrics;
using System.Reflection;

using Microsoft.Extensions.Logging;

// TODO
#pragma warning disable CA1823
// ReSharper disable NotAccessedField.Local
// ReSharper disable UnusedMember.Local
internal sealed class HyperVMetrics
{
    internal static readonly AssemblyName AssemblyName = typeof(HyperVMetrics).Assembly.GetName();
    internal static readonly string MeterName = AssemblyName.Name!.Replace(".Instrumentation", string.Empty, StringComparison.Ordinal);

    private static readonly Meter MeterInstance = new(MeterName, AssemblyName.Version!.ToString());

    private readonly int cacheDuration;

    private readonly string host;

    public HyperVMetrics(
        ILogger<HyperVMetrics> log,
        HyperVOptions options)
    {
        log.InfoMetricsEnabled(nameof(HyperVMetrics));

        cacheDuration = options.CacheDuration;
        // TODO ignore
        host = options.Host;

        // TODO
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    // TODO

    //--------------------------------------------------------------------------------
    // Measure
    //--------------------------------------------------------------------------------

    // TODO

    //--------------------------------------------------------------------------------
    // VirtualMachine
    //--------------------------------------------------------------------------------

    // TODO
}

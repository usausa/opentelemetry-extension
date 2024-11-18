namespace OpenTelemetryExtension.Instrumentation.Ble;

using System.Diagnostics.Metrics;
using System.Reflection;

using Microsoft.Extensions.Logging;

// TODO
#pragma warning disable CA1823
// ReSharper disable NotAccessedField.Local
// ReSharper disable UnusedMember.Local
internal sealed class BleMetrics : IDisposable
{
    internal static readonly AssemblyName AssemblyName = typeof(BleMetrics).Assembly.GetName();
    internal static readonly string MeterName = AssemblyName.Name!.Replace(".Instrumentation", string.Empty, StringComparison.Ordinal);

    private static readonly Meter MeterInstance = new(MeterName, AssemblyName.Version!.ToString());

    private readonly string host;

    public BleMetrics(
        ILogger<BleMetrics> log,
        BleOptions options)
    {
        log.InfoMetricsEnabled(nameof(BleMetrics));

        host = options.Host;

        // TODO
    }

    public void Dispose()
    {
        // TODO
    }

    //--------------------------------------------------------------------------------
    // Measure
    //--------------------------------------------------------------------------------

    // TODO

    //--------------------------------------------------------------------------------
    // Device
    //--------------------------------------------------------------------------------

    // TODO
}
